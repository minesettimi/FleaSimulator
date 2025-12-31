using System.Reflection;
using FleaSimulator.Models.Config;
using FleaSimulator.Models.State;
using FleaSimulator.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Eft.Ragfair;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace FleaSimulator.Helpers;

[Injectable(InjectionType.Singleton)]
public class SellHelper(PresetService presetService,
    ItemDataService itemService,
    HttpResponseUtil httpResponseUtil,
    RagfairOfferHelper offerHelper,
    RagfairController ragfairController,
    ItemHelper itemHelper,
    ConfigServer configServer,
    ChaosHelper chaosHelper,
    InventoryHelper inventoryHelper,
    RagfairOfferHolder offerHolder,
    MathUtil mathUtil,
    RagfairSellHelper sellHelper,
    TimeUtil timeUtil,
    RandomUtil randomUtil,
    RagfairPriceService priceService,
    ISptLogger<SellHelper> logger)
{
    
    //I would like to not have to recreate all of these functions but the functions I want to override are passed no item data
    private readonly RagfairConfig _ragfairConfig = configServer.GetConfig<RagfairConfig>();
    
    private readonly MethodBase _getFromInventory = typeof(RagfairController)
        .GetMethod("GetItemsToListOnFleaFromInventory", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private readonly MethodBase _createPlayerOffer = typeof(RagfairController)
        .GetMethod("CreatePlayerOffer", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private readonly MethodBase _calculateRequirements = typeof(RagfairController)
        .GetMethod("CalculateRequirementsPriceInRub", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private readonly MethodBase _chargePlayerTaxFee = typeof(RagfairController)
        .GetMethod("ChargePlayerTaxFee", BindingFlags.Instance | BindingFlags.NonPublic)!;
    
    private readonly Type _getItemsToList = typeof(RagfairController)
        .GetNestedType("GetItemsToListOnFleaFromInventoryResult",
            BindingFlags.Instance | BindingFlags.NonPublic)!;
    
    public ItemEventRouterResponse? CreatePackOffer(
        MongoId sessionID,
        AddOfferRequestData offerRequest,
        SptProfile fullProfile,
        ItemEventRouterResponse output
        )
    {
        PmcData pmcData = fullProfile.CharacterData.PmcData;

        List<Item> firstItem = pmcData.Inventory.Items.GetItemWithChildren(offerRequest.Items.FirstOrDefault());
        
        itemService.CurrentState.Items.TryGetValue(firstItem[0].Template, out ItemState? firstItemState);

        if (firstItemState is null)
            return null;

        GetItemsToList result = GetItemsFromInventory(pmcData, offerRequest.Items);
        if (result.Items is null || !string.IsNullOrEmpty(result.ErrorMessage))
        {
            httpResponseUtil.AppendErrorToOutput(output, result.ErrorMessage);
        }

        double stackTotal = offerHelper.GetTotalStackCountSize(result.Items);

        Item? firstInvItem = firstItem.FirstOrDefault();
        firstInvItem.Upd ??= new Upd();
        firstInvItem.Upd.StackObjectsCount = stackTotal;

        RagfairOffer offer = (RagfairOffer)_createPlayerOffer.Invoke(ragfairController,
            [sessionID, offerRequest.Requirements, firstItem, true])!;

        Item newRootItem = offer.Items[0];

        double qualityMultiplier = itemHelper.GetItemQualityModifierForItems(offer.Items, true);

        double playerPriceRub = (double)_calculateRequirements.Invoke(ragfairController, [offerRequest.Requirements])!;

        double sellChance = CalculateSellChance(newRootItem.Template, firstItemState,
            playerPriceRub / stackTotal, qualityMultiplier, out int offerPos);

        List<SellResult> initialResults = sellHelper.RollForSale(sellChance, (int)stackTotal, true);
        offer.SellResults = SetOfferDelays(firstItemState, initialResults, offerPos);

        if (_ragfairConfig.Sell.Fees)
        {
            bool feeChargeFailed = (bool)_chargePlayerTaxFee.Invoke(ragfairController, [
                sessionID,
                newRootItem,
                pmcData,
                playerPriceRub,
                (int)stackTotal,
                offerRequest,
                output
            ])!;

            if (feeChargeFailed)
            {
                return output;
            }
        }
        
        fullProfile.CharacterData.PmcData.RagfairInfo.Offers.Add(offer);
        output.ProfileChanges[sessionID].RagFairOffers.Add(offer);

        foreach (MongoId removeItem in offerRequest.Items)
        {
            inventoryHelper.RemoveItem(pmcData, removeItem, sessionID, output);
        }

        return output;
    }
    
    public ItemEventRouterResponse? CreateMultiOffer(
        MongoId sessionID,
        AddOfferRequestData offerRequest,
        SptProfile fullProfile,
        ItemEventRouterResponse output
        )
    {
        PmcData pmcData = fullProfile.CharacterData.PmcData;

        MongoId firstOfferItemId = offerRequest.Items.First();
        
        itemService.CurrentState.Items.TryGetValue(firstOfferItemId, out ItemState? firstItemState);

        if (firstItemState is null)
            return null;
        
        List<Item> inventoryItems = pmcData.Inventory.Items.GetItemWithChildren(firstOfferItemId);

        GetItemsToList result = GetItemsFromInventory(pmcData, offerRequest.Items);
        if (result.Items is null || !string.IsNullOrEmpty(result.ErrorMessage))
        {
            httpResponseUtil.AppendErrorToOutput(output, result.ErrorMessage);
        }

        double stackTotal = offerHelper.GetTotalStackCountSize(result.Items);

        Item? firstInvItem = inventoryItems.FirstOrDefault();
        firstInvItem.Upd ??= new Upd();
        firstInvItem.Upd.StackObjectsCount = stackTotal;

        RagfairOffer offer = (RagfairOffer)_createPlayerOffer.Invoke(ragfairController,
            [sessionID, offerRequest.Requirements, inventoryItems, true])!;

        Item newRootItem = offer.Items[0];

        double qualityMultiplier = itemHelper.GetItemQualityModifierForItems(offer.Items, true);

        double playerPriceRub = (double)_calculateRequirements.Invoke(ragfairController, [offerRequest.Requirements])!;
        
        double sellChance = CalculateSellChance(newRootItem.Template, firstItemState,
            playerPriceRub / stackTotal, qualityMultiplier, out int offerPos);

        List<SellResult> initialResults = sellHelper.RollForSale(sellChance, (int)stackTotal, true);
        offer.SellResults = SetOfferDelays(firstItemState, initialResults, offerPos);

        if (_ragfairConfig.Sell.Fees)
        {
            bool feeChargeFailed = (bool)_chargePlayerTaxFee.Invoke(ragfairController, [
                sessionID,
                newRootItem,
                pmcData,
                playerPriceRub,
                (int)stackTotal,
                offerRequest,
                output
            ])!;

            if (feeChargeFailed)
            {
                return output;
            }
        }
        
        fullProfile.CharacterData.PmcData.RagfairInfo.Offers.Add(offer);
        output.ProfileChanges[sessionID].RagFairOffers.Add(offer);

        foreach (MongoId removeItem in offerRequest.Items)
        {
            inventoryHelper.RemoveItem(pmcData, removeItem, sessionID, output);
        }

        return output;
    }
    
    public ItemEventRouterResponse? CreateSingleOffer(
        MongoId sessionID,
        AddOfferRequestData offerRequest,
        SptProfile fullProfile,
        ItemEventRouterResponse output
        )
    {
        PmcData pmcData = fullProfile.CharacterData.PmcData;
        
        GetItemsToList itemsToSell = GetItemsFromInventory(pmcData, offerRequest.Items);
        if (itemsToSell.Items is null || !string.IsNullOrEmpty(itemsToSell.ErrorMessage))
        {
            httpResponseUtil.AppendErrorToOutput(output, itemsToSell.ErrorMessage);
        }

        Item firstItem = itemsToSell.Items.FirstOrDefault().FirstOrDefault()!;
        
        itemService.CurrentState.Items.TryGetValue(firstItem.Template, out ItemState? firstItemState);

        if (firstItemState is null)
            return null;

        double stackTotal = offerHelper.GetTotalStackCountSize(itemsToSell.Items);

        RagfairOffer offer = (RagfairOffer)_createPlayerOffer.Invoke(ragfairController,
            [sessionID, offerRequest.Requirements, firstItem, false])!;

        Item newRootItem = offer.Items.FirstOrDefault(x => x.Id == offerRequest.Items[0]);

        double qualityMultiplier = itemHelper.GetItemQualityModifierForItems(offer.Items, true);

        double playerPriceRub = (double)_calculateRequirements.Invoke(ragfairController, [offerRequest.Requirements])!;
        
        
        double sellChance = CalculateSellChance(newRootItem.Template, firstItemState,
            playerPriceRub / stackTotal, qualityMultiplier, out int offerPos);

        List<SellResult> initialResults = sellHelper.RollForSale(sellChance, (int)stackTotal, true);
        offer.SellResults = SetOfferDelays(firstItemState, initialResults, offerPos);

        if (_ragfairConfig.Sell.Fees)
        {
            bool feeChargeFailed = (bool)_chargePlayerTaxFee.Invoke(ragfairController, [
                sessionID,
                newRootItem,
                pmcData,
                playerPriceRub,
                (int)stackTotal,
                offerRequest,
                output
            ])!;

            if (feeChargeFailed)
            {
                return output;
            }
        }
        
        fullProfile.CharacterData.PmcData.RagfairInfo.Offers.Add(offer);
        output.ProfileChanges[sessionID].RagFairOffers.Add(offer);

        foreach (MongoId removeItem in offerRequest.Items)
        {
            inventoryHelper.RemoveItem(pmcData, removeItem, sessionID, output);
        }

        return output;
    }

    private double CalculateSellChance(MongoId tpl, ItemState itemState, double singleItemPrice, double quality,
        out int resultPos, double presetPrice = -1)
    {
        CategoryConfig category = itemState.Category;
        
        SellConfig sellConfig = presetService.Config.Core.SellConfig;

        int maxSellPos =
            (int)Math.Floor(chaosHelper.MapToRange01(category.Supply, sellConfig.SupplyMinPosition, sellConfig.SupplyMaxPosition));

        int maxChance = (int)Math.Floor(chaosHelper.MapToRange01(category.Demand, sellConfig.DemandMinBuyChance,
            sellConfig.DemandMaxBuyChance));
        
        //get item position
        IEnumerable<RagfairOffer> offers = offerHolder.GetOffersByTemplate(tpl)!;
        List<MongoId> expiredOffers = offerHolder.GetStaleOfferIds();

        double totalPresetPrice = 0.0;

        int validOffers = 0;
        int position = 0;
        foreach (RagfairOffer offer in offers)
        {
            if (!offer.IsFakePlayerOffer() || offer.IsTraderOffer() || expiredOffers.Contains(offer.Id))
                continue;

            validOffers++;

            double offerPreset = 0;

            if (presetPrice >= 1 && itemHelper.IsOfBaseclass(tpl, BaseClasses.WEAPON))
                offerPreset = priceService.GetPresetPriceByChildren(offer.Items!);

            totalPresetPrice += offerPreset;
            
            double offerQuality = itemHelper.GetItemQualityModifierForItems(offer.Items!, true);

            //move the position back for each better 
            if (offer.RequirementsCost < singleItemPrice || offerQuality > quality)
                position++;
        }

        double presetMod = 1.0;

        if (presetPrice >= 1)
        {
            double presetAvg = totalPresetPrice / validOffers;
            presetMod += (presetPrice - presetAvg) / presetAvg; //negative numbers when lower, positive when higher
        }

        double sellChance = mathUtil.MapToRange(position, 0, maxSellPos, maxChance, 0);

        resultPos = position;

        return chaosHelper.ChaosShift(category, sellChance) * quality * presetMod;
    }

    private List<SellResult> SetOfferDelays(ItemState itemState, List<SellResult> results, int position)
    {
        long currentTimestamp = timeUtil.GetTimeStamp();
        SellConfig sellConfig = presetService.Config.Core.SellConfig;
        
        logger.Info($"[FleaSimulator] There are {results.Count} sell results.");
        
        double maxDelay = chaosHelper.MapToRange01(itemState.Category.Demand, sellConfig.DemandMinDelay, sellConfig.DemandMaxDelay);
        maxDelay += sellConfig.PosDelay * position;

        maxDelay = chaosHelper.ChaosShift(itemState.Category, maxDelay);

        foreach (SellResult result in results)
        {
            int delay = (int)Math.Round(randomUtil.GetBiasedRandomNumber(0, maxDelay, 0, 6));

            result.SellTime = currentTimestamp + delay;
        }

        return results;
    }

    private GetItemsToList GetItemsFromInventory(PmcData pmcData, List<MongoId> items)
    {
        object newList = _getFromInventory.Invoke(pmcData, [pmcData, items])!;

        PropertyInfo itemProperty = _getItemsToList.GetProperty("Items", BindingFlags.Instance)!;
        PropertyInfo errorProperty = _getItemsToList.GetProperty("ErrorMessage", BindingFlags.Instance)!;

        GetItemsToList convertedList = new()
        {
            Items = (List<List<Item>>)itemProperty.GetValue(newList)!,
            ErrorMessage = (string?)errorProperty.GetValue(newList)!
        };

        return convertedList;
    }

    public record GetItemsToList
    {
        public List<List<Item>>? Items { get; set; }
        public string? ErrorMessage { get; set; }
    }
}