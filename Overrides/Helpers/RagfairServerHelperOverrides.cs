using System.Reflection;
using FleaSimulator.Helpers;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;

namespace FleaSimulator.Overrides.Helpers;

public class CalculateStackCountOverride : AbstractPatch
{
    private static OfferHelper offerHelper;
    
    protected override MethodBase? GetTargetMethod()
    {
        offerHelper = ServiceLocator.ServiceProvider.GetRequiredService<OfferHelper>();
        return typeof(RagfairServerHelper).GetMethod(nameof(RagfairServerHelper.CalculateDynamicStackCount));
    }

    [PatchPrefix]
    public static bool Prefix(MongoId tplId, bool isPreset, ref int __result)
    {
        if (!offerHelper.ShouldModifyQuantity(tplId, isPreset))
            return true;

        int quant = offerHelper.GetItemQuantity(tplId);

        //failed, run actual func
        if (quant < 1)
            return true;
        
        __result = quant;
        
        return false;
    }

    [PatchPostfix]
    public static int Postfix(int __result)
    {
        return offerHelper.ModifyWipeQuantity(__result);
    }
}