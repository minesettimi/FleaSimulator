using System.Reflection;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace FleaSimulator.Overrides.Generators;

public class GenerateDynamicOffersOverride : AbstractPatch
{
    protected override MethodBase? GetTargetMethod()
    {
        return typeof(RagfairOfferGenerator).GetMethod(nameof(RagfairOfferGenerator.GenerateDynamicOffers));
    }

    [PatchPrefix]
    public static void Prefix(IEnumerable<List<Item>>? expiredOffers = null)
    {
        
    }
}