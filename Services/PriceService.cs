using System.Reflection;
using FleaSimulator.Models.Config;
using FleaSimulator.Models.State;
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

namespace FleaSimulator.Services;

[Injectable(InjectionType.Singleton)]
public class PriceService(DatabaseService database,
    ItemDataService dataService,
    MathUtil mathUtil,
    RagfairPriceService ragfairPriceService,
    ConfigServer configServer,
    PresetHelper presetHelper,
    HandbookHelper handbookHelper,
    RandomUtil randomUtil,
    ItemDataService itemService,
    ItemHelper itemHelper,
    ISptLogger<PriceService> logger
    )
{
    private readonly RagfairConfig _ragfairConfig = configServer.GetConfig<RagfairConfig>();
    private MethodBase _getWeaponPresetprice = typeof(RagfairPriceService)
        .GetMethod("GetWeaponPresetPrice", BindingFlags.Instance | BindingFlags.NonPublic)!;
    
    public void UpdatePrices()
    {
        logger.Info("[FleaSimulator] Updating offer prices.");

        Dictionary<MongoId, double> priceTable = database.GetPrices();
        
        SaveState currentState = dataService.CurrentState;
        
        //update state
        currentState.UpdateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        dataService.SaveCurrentState();

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

    public double GetItemPrice(MongoId tplId,
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
        
        ItemState itemState = itemService.CurrentState.Items[tplId];
        CategoryConfig category = itemState.Category;

        double minPrice = category.MinOfferPrice * 100;
        double maxPrice = category.MaxOfferPrice * 100;

        double multiplier = randomUtil.GetBiasedRandomNumber(minPrice, 
            maxPrice,
            maxPrice - minPrice - category.Chaos,
            2d + category.Chaos*2);
        
        price *= multiplier / 100;

        if (desiredCurrency != Money.ROUBLES)
        {
            price = handbookHelper.FromRoubles(price, desiredCurrency);
        }
        
        return price <= 0 ? 0.1d : price;
    }
}