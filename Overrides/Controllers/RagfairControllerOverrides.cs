using System.Reflection;
using FleaSimulator.Helpers;
using FleaSimulator.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.Controllers;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.ItemEvent;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Eft.Ragfair;

namespace FleaSimulator.Overrides.Controllers;

[Injectable]
public class CreatePackOfferOverride : AbstractPatch
{
    private static PresetService _presetService = null!;
    private static SellHelper _sellHelper = null!;

    public CreatePackOfferOverride(PresetService presetService, SellHelper sellHelper)
    {
        _presetService = presetService;
        _sellHelper = sellHelper;
    }

    protected override MethodBase? GetTargetMethod()
    {
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
        if (!_presetService.Config.Core.SellConfig.Enabled)
            return true;
        
        ItemEventRouterResponse? result = _sellHelper.CreatePackOffer(sessionID, offerRequest, fullProfile, output);

        //item is not present in plugin data, run as normal
        if (result == null)
            return true;

        __result = result;
        
        return false;
    }
}

[Injectable]
public class CreateMultiOfferOverride : AbstractPatch
{
    private static PresetService _presetService = null!;
    private static SellHelper _sellHelper = null!;
    
    public CreateMultiOfferOverride(PresetService presetService, SellHelper sellHelper)
    {
        _presetService = presetService;
        _sellHelper = sellHelper;
    }

    protected override MethodBase? GetTargetMethod()
    {
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
        if (!_presetService.Config.Core.SellConfig.Enabled)
            return true;
        
        ItemEventRouterResponse? result = _sellHelper.CreateMultiOffer(sessionID, offerRequest, fullProfile, output);
        
        if (result == null)
            return true;

        __result = result;
        
        return false;
    }
}

[Injectable]
public class CreateSingleOfferOverride : AbstractPatch
{
    private static PresetService _presetService;
    private static SellHelper _sellHelper;

    public CreateSingleOfferOverride(PresetService presetService, SellHelper sellHelper)
    {
        _presetService = presetService;
        _sellHelper = sellHelper;
    }
        
    protected override MethodBase? GetTargetMethod()
    {
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
        if (!_presetService.Config.Core.SellConfig.Enabled)
            return true;

        ItemEventRouterResponse? result = _sellHelper.CreateSingleOffer(sessionID, offerRequest, fullProfile, output);
        
        if (result == null)
            return true;

        __result = result;
        
        return false;
    }
}