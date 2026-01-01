using System.Reflection;
using FleaSimulator.Services;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Ragfair;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;

namespace FleaSimulator.Overrides.Servers;

public class UpdateOverride : AbstractPatch
{
    private static PresetService _presetService;
    private static long lastUpdate;
    
    protected override MethodBase? GetTargetMethod()
    {
        _presetService = ServiceLocator.ServiceProvider.GetRequiredService<PresetService>()!;
        return typeof(RagfairServer).GetMethod(nameof(RagfairServer.Update));
    }

    [PatchPrefix]
    public static bool Prefix(RagfairServer __instance)
    {
        if (!_presetService.Config.Core.BuyConfig.Enabled)
            return true;
        
        long now = DateTimeOffset.Now.ToUnixTimeSeconds();

        if (now < lastUpdate + _presetService.Config.Core.UpdateInterval * 60)
            return false;
        
        lastUpdate = now;
        
        //kill off every fake flea offer before processing
        RagfairOfferHolder offerHolder = ServiceLocator.ServiceProvider.GetRequiredService<RagfairOfferHolder>();
        List<MongoId> expired = offerHolder.GetStaleOfferIds();
        
        //credit to DrakiaXYZ
        foreach (RagfairOffer offer in offerHolder.GetOffers())
        {
            if (offer.IsPlayerOffer() || offer.IsTraderOffer() || expired.Contains(offer.Id))
                continue;
            
            offerHolder.FlagOfferAsExpired(offer.Id);
        }
        
        return true;
    }
}