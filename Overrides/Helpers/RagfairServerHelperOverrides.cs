using System.Reflection;
using FleaSimulator.Helpers;
using FleaSimulator.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Helpers.Ragfair;
using SPTarkov.Server.Core.Models.Common;

namespace FleaSimulator.Overrides.Helpers;

[Injectable]
public class CalculateStackCountOverride : AbstractPatch
{
    private static OfferHelper _offerHelper;
    private static PresetService _presetService;

    public CalculateStackCountOverride(OfferHelper offerHelper, PresetService presetService)
    {
        _offerHelper = offerHelper;
        _presetService = presetService;
    }
    
    protected override MethodBase? GetTargetMethod()
    {
        return typeof(RagfairServerHelper).GetMethod(nameof(RagfairServerHelper.CalculateDynamicStackCount));
    }

    [PatchPrefix]
    public static bool Prefix(MongoId tplId, bool isPreset, ref int __result)
    {
        if (!_presetService.Config.Core.BuyConfig.Enabled ||
            !_offerHelper.ShouldModifyQuantity(tplId, isPreset))
            return true;

        int quant = _offerHelper.GetItemQuantity(tplId);

        //failed, run actual func
        if (quant < 1)
            return true;
        
        __result = quant;
        
        return false;
    }

    [PatchPostfix]
    public static int Postfix(int __result)
    {
        return !_presetService.Config.Core.BuyConfig.Enabled ? __result : _offerHelper.ModifyWipeQuantity(__result);
    }
}