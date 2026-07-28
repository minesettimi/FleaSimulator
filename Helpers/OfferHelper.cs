using System.Reflection;
using FleaSimulator.Models.Config;
using FleaSimulator.Models.State;
using FleaSimulator.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace FleaSimulator.Helpers;

[Injectable(InjectionType.Singleton)]
public class OfferHelper(DatabaseService database,
    ItemDataService dataService,
    MathUtil mathUtil,
    RagfairPriceService ragfairPriceService,
    ConfigServer configServer,
    PresetHelper presetHelper,
    HandbookHelper handbookHelper,
    RandomUtil randomUtil,
    ItemDataService itemService,
    ItemHelper itemHelper,
    PresetService presetService,
    ChaosHelper chaosHelper,
    ISptLogger<OfferHelper> logger
    )
{
    private readonly MethodBase _getWeaponPresetprice = typeof(RagfairPriceService)
        .GetMethod("GetWeaponPresetPrice", BindingFlags.Instance | BindingFlags.NonPublic)!;

    private readonly RagfairConfig _ragfairConfig = configServer.GetConfig<RagfairConfig>();

    public void UpdatePrices(bool onlyTime)
    {
        if (!presetService.Config.Core.Simulate)
            return;
            
        SaveState currentState = dataService.CurrentState;
        
        //update state
        currentState.UpdateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        dataService.SaveCurrentState();

        if (onlyTime)
            return;
        
        logger.Info("[FleaSimulator] Updating offer prices.");

        Dictionary<MongoId, double> priceTable = database.GetPrices();

        //update all prices
        Dictionary<MongoId, ItemState> itemStates = dataService.CurrentState.Items;
        foreach (KeyValuePair<MongoId, ItemState> states in itemStates)
        {
            double minPrice = Math.Min(states.Value.CurrentPrice, states.Value.TargetPrice);
            double maxPrice = Math.Max(states.Value.CurrentPrice, states.Value.TargetPrice);
            
            priceTable[states.Key] = Math.Round(mathUtil.MapToRange(currentState.UpdateTime, currentState.LastUpdate,
                currentState.NextUpdate,minPrice, maxPrice));
        }
    }

    public double? GetItemPrice(MongoId tplId,
        MongoId desiredCurrency,
        Item? item,
        IEnumerable<Item>? offerItems,
        bool? isPackOffer
        )
    {
        double price = ragfairPriceService.GetFleaPriceForItem(tplId);

        if (item?.Upd?.SptPresetId is not null
            && offerItems is not null
            && presetHelper.IsPresetBaseClass(item.Upd.SptPresetId.Value, BaseClasses.WEAPON))
        {
            price = (double)_getWeaponPresetprice.Invoke(ragfairPriceService, [item, offerItems, price])!;
        }

        if (item is not null && !_ragfairConfig.Dynamic.IgnoreQualityPriceVarianceBlacklist.Contains(tplId))
        {
            double qualityModifier = itemHelper.GetItemQualityModifier(item);
            price *= qualityModifier;
        }
        
        itemService.CurrentState.Items.TryGetValue(tplId, out ItemState? itemState);

        if (itemState is null)
        {
            return null;
        }
        
        CategoryConfig category = itemState.Category;

        double minPrice = category.MinOfferPrice * 100;
        double maxPrice = category.MaxOfferPrice * 100;

        double dif = maxPrice - minPrice;
        double middle = dif * 0.5;
        
        double multiplier = randomUtil.GetNormallyDistributedRandomNumber(minPrice + middle, middle * 0.5 - 1);
        
        price *= multiplier / 100;

        if (desiredCurrency != Money.ROUBLES)
        {
            price = handbookHelper.FromRoubles(price, desiredCurrency);
        }
        
        return price <= 0 ? 0.1d : price;
    }

    public bool ShouldModifyQuantity(MongoId tplId, bool isPreset)
    {
        KeyValuePair<bool, TemplateItem?> itemDetails = itemHelper.GetItem(tplId);
        if (!itemDetails.Key)
            return false;

        if (isPreset || itemHelper.IsOfBaseclasses(itemDetails.Value!.Id, _ragfairConfig.Dynamic.ShowAsSingleStack))
            return false;

        int stackSize = itemDetails.Value?.Properties?.StackMaxSize ?? 1;
        
        return stackSize == 1;
    }

    public int GetItemQuantity(MongoId tplId)
    {
        ItemState? itemState = itemService.CurrentState.Items.GetValueOrDefault(tplId);

        if (itemState is null)
            return -1;
        
        CategoryConfig category = itemState.Category;
        BuyConfig buyConfig = presetService.Config.Core.BuyConfig;

        double configQuantity = chaosHelper.MapToRange01(category.Supply, buyConfig.SupplyMinQuantity, buyConfig.SupplyMaxQuantity);

        double altered = randomUtil.GetBiasedRandomNumber(1.0, configQuantity + 1d, configQuantity - 2d, 4.0);

        return (int)Math.Max(Math.Floor(altered), 1);
    }

    public int ModifyWipeQuantity(int quantity)
    {
        if (dataService.CurrentState.WipeState == WipeState.Middle)
            return quantity;

        double resultQuantity = quantity * presetService.Config.Core.WipePrices.EarlyQuantityMult;
        
        return (int)Math.Max(Math.Round(resultQuantity), 1);
    }
}