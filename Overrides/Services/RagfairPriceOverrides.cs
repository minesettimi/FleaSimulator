using System.Reflection;
using FleaSimulator.Helpers;
using FleaSimulator.Services;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Ragfair;

namespace FleaSimulator.Overrides.Services;


public class GetDynamicPriceOverride : AbstractPatch
{
    private static OfferHelper _offerHelper = null!;
    private static PresetService _presetService = null!;

    public GetDynamicPriceOverride(OfferHelper offerHelper, PresetService presetService)
    {
        _offerHelper = offerHelper;
        _presetService = presetService;
    }
    
    protected override MethodBase? GetTargetMethod()
    {
        return typeof(RagfairPriceService).GetMethod(nameof(RagfairPriceService.GetDynamicItemPrice));
    }

    //can't override the middle and the functions used don't have enough params, replace it
    [PatchPrefix]
    public static bool Prefix(MongoId itemTemplateId,
        MongoId desiredCurrency,
        Item? item,
        IEnumerable<Item>? offerItems,
        bool? isPackOffer,
        ref double? __result
        )
    {
        if (!_presetService.Config.Core.BuyConfig.Enabled)
            return true;
            
        double? price = _offerHelper.GetItemPrice(itemTemplateId, desiredCurrency, item, offerItems, isPackOffer);

        //item was invalid or some other issue, run original service.
        if (price is null)
            return true;
        
        __result = price;
        
        return false;
    }
}