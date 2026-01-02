using System.Reflection;
using FleaSimulator.Generators;
using FleaSimulator.Helpers;
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
        
        //if the generator failed, use default functionality
        return !generator.CreateOffersFromAssorts(assortItemWithChildren, isExpiredOffer);;
    }
}

public class GenerateDynamicOffersOverride : AbstractPatch
{
    private static OfferHelper offerHelper;
    private static PresetService presetService;
    
    protected override MethodBase? GetTargetMethod()
    {
        presetService = ServiceLocator.ServiceProvider.GetService<PresetService>();
        offerHelper = ServiceLocator.ServiceProvider.GetService<OfferHelper>()!;
        return typeof(RagfairOfferGenerator).GetMethod(nameof(RagfairOfferGenerator.GenerateDynamicOffers));
    }

    [PatchPrefix]
    public static bool Prefix()
    {
        if (!presetService.Config.Core.BuyConfig.Enabled)
            return true;
        
        offerHelper.UpdatePrices();
        
        return true;
    }
}
