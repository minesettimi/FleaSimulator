using System.Reflection;
using FleaSimulator.Generators;
using FleaSimulator.Models.State;
using FleaSimulator.Services;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace FleaSimulator.Overrides.Generators;

public class CreateOffersFromAssortOverride : AbstractPatch
{
    private static FleaOfferGenerator generator;
    private static PresetService presetService;
    
    protected override MethodBase? GetTargetMethod()
    {
        generator = ServiceLocator.ServiceProvider.GetService<FleaOfferGenerator>()!;
        presetService = ServiceLocator.ServiceProvider.GetService<PresetService>()!;
        return typeof(RagfairOfferGenerator).GetMethod("CreateOffersFromAssort", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    [PatchPrefix]
    public static bool Prefix(List<Item> assortItemWithChildren, bool isExpiredOffer, Dynamic config)
    {
        if (!presetService.Config.Core.BuyConfig.Enabled)
            return true;
        
        generator.CreateOffersFromAssorts(assortItemWithChildren);
        
        return false;
    }
}

public class GenerateDynamicOffersOverride : AbstractPatch
{
    private static PriceService priceService;
    
    protected override MethodBase? GetTargetMethod()
    {
        priceService = ServiceLocator.ServiceProvider.GetService<PriceService>()!;
        return typeof(RagfairOfferGenerator).GetMethod(nameof(RagfairOfferGenerator.GenerateDynamicOffers));
    }

    [PatchPrefix]
    public static bool Prefix()
    {
        priceService.UpdatePrices();
        
        return true;
    }
}
