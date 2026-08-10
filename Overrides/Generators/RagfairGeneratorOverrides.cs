using System.Reflection;
using FleaSimulator.Generators;
using FleaSimulator.Helpers;
using FleaSimulator.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Generators.Ragfair;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;

namespace FleaSimulator.Overrides.Generators;

[Injectable]
public class CreateOffersFromAssortOverride : AbstractPatch
{
    private static FleaOfferGenerator _generator = null!;
    private static PresetService _presetService = null!;

    public CreateOffersFromAssortOverride(FleaOfferGenerator offerGenerator, PresetService presetService)
    {
        _generator = offerGenerator;
        _presetService = presetService;
    }

    protected override MethodBase? GetTargetMethod()
    {
        return typeof(RagfairOfferGenerator).GetMethod("CreateOffersFromAssort", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    [PatchPrefix]
    public static bool Prefix(List<Item> assortItemWithChildren, bool isExpiredOffer, Dynamic config)
    {
        if (!_presetService.Config.Core.BuyConfig.Enabled)
            return true;
        
        //if the generator failed, use default functionality
        return !_generator.CreateOffersFromAssorts(assortItemWithChildren, isExpiredOffer);;
    }
}

[Injectable]
public class GenerateDynamicOffersOverride : AbstractPatch
{
    private static OfferHelper _offerHelper = null!;
    private static PresetService _presetService = null!;

    public GenerateDynamicOffersOverride(OfferHelper offerHelper, PresetService presetService)
    {
        _offerHelper = offerHelper;
        _presetService = presetService;
    }
    
    protected override MethodBase? GetTargetMethod()
    {
        return typeof(RagfairOfferGenerator).GetMethod(nameof(RagfairOfferGenerator.GenerateDynamicOffers));
    }

    [PatchPrefix]
    public static bool Prefix(ref IEnumerable<List<Item>>? expiredOffers)
    {
        if (!_presetService.Config.Core.BuyConfig.Enabled)
        {
            _offerHelper.UpdatePrices(true); //updateTime should be updated still
            return true;
        }
        
        _offerHelper.UpdatePrices(false);
        expiredOffers = null;
        
        return true;
    }
}
