using System.Reflection;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Services;

namespace FleaSimulator.Overrides.Services;


public class GetDynamicOfferPriceOverride : AbstractPatch
{
    protected override MethodBase? GetTargetMethod()
    {
        return typeof(RagfairPriceService).GetMethod(nameof(RagfairPriceService.GetDynamicItemPrice));
    }
}