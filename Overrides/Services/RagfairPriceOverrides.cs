using System.Reflection;
using FleaSimulator.Services;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;

namespace FleaSimulator.Overrides.Services;


public class GetDynamicPriceOverride : AbstractPatch
{
    private static PriceService priceService;
    private static PresetService presetService;
    
    protected override MethodBase? GetTargetMethod()
    {
        priceService = ServiceLocator.ServiceProvider.GetService<PriceService>()!;
        presetService = ServiceLocator.ServiceProvider.GetService<PresetService>()!;
        return typeof(RagfairPriceService).GetMethod(nameof(RagfairPriceService.GetDynamicItemPrice));
    }
    
    //can't override the middle and the functions used don't have enough params, replace it
    [PatchPrefix]
    public static bool PatchPrefixAttribute(MongoId tplId,
        MongoId desiredCurrency,
        Item? item,
        IEnumerable<Item>? offerItems,
        bool? isPackOffer,
        ref double __result
        )
    {
        if (!presetService.Config.Core.BuyConfig.Enabled)
            return true;
            
        __result = priceService.GetItemPrice(tplId, desiredCurrency, item, offerItems, isPackOffer);
        
        return false;
    }
}