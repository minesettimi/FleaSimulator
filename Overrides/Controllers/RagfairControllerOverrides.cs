using System.Reflection;
using FleaSimulator.Helpers;
using FleaSimulator.Services;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Eft.Ragfair;

namespace FleaSimulator.Overrides.Controllers;

public class CreatePackOfferOverride : AbstractPatch
{
    private static PresetService presetService;
    private static SellHelper sellHelper;

    protected override MethodBase? GetTargetMethod()
    {
        presetService = ServiceLocator.ServiceProvider.GetRequiredService<PresetService>();
        sellHelper = ServiceLocator.ServiceProvider.GetRequiredService<SellHelper>();
        return typeof(RagfairController).GetMethod("CreatePackOffer", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    [PatchPrefix]
    public static bool Prefix(
        MongoId sessionID,
        AddOfferRequestData offerRequest,
        SptProfile fullProfile,
        ItemEventRouterResponse output,
        ref ItemEventRouterResponse __result
    )
    {
        if (!presetService.Config.Core.SellConfig.Enabled)
            return true;
        
        ItemEventRouterResponse? result = sellHelper.CreatePackOffer(sessionID, offerRequest, fullProfile, output);

        //item is not present in plugin data, run as normal
        if (result == null)
            return true;

        __result = result;
        
        return false;
    }
}

public class CreateMultiOfferOverride : AbstractPatch
{
    private static PresetService presetService;
    private static SellHelper sellHelper;

    protected override MethodBase? GetTargetMethod()
    {
        presetService = ServiceLocator.ServiceProvider.GetRequiredService<PresetService>();
        sellHelper = ServiceLocator.ServiceProvider.GetRequiredService<SellHelper>();
        return typeof(RagfairController).GetMethod("CreateMultiOffer", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    [PatchPrefix]
    public static bool Prefix(
        MongoId sessionID,
        AddOfferRequestData offerRequest,
        SptProfile fullProfile,
        ItemEventRouterResponse output,
        ref ItemEventRouterResponse __result
    )
    {
        if (!presetService.Config.Core.SellConfig.Enabled)
            return true;
        
        ItemEventRouterResponse? result = sellHelper.CreateMultiOffer(sessionID, offerRequest, fullProfile, output);
        
        if (result == null)
            return true;

        __result = result;
        
        return false;
    }
}

public class CreateSingleOfferOverride : AbstractPatch
{
    private static PresetService presetService;
    private static SellHelper sellHelper;

    protected override MethodBase? GetTargetMethod()
    {
        presetService = ServiceLocator.ServiceProvider.GetRequiredService<PresetService>();
        sellHelper = ServiceLocator.ServiceProvider.GetRequiredService<SellHelper>();
        return typeof(RagfairController).GetMethod("CreateSingleOffer", BindingFlags.Instance | BindingFlags.NonPublic);
    }

    [PatchPrefix]
    public static bool Prefix(
        MongoId sessionID,
        AddOfferRequestData offerRequest,
        SptProfile fullProfile,
        ItemEventRouterResponse output,
        ref ItemEventRouterResponse __result
    )
    {
        if (!presetService.Config.Core.SellConfig.Enabled)
            return true;

        ItemEventRouterResponse? result = sellHelper.CreateSingleOffer(sessionID, offerRequest, fullProfile, output);
        
        if (result == null)
            return true;

        __result = result;
        
        return false;
    }
}