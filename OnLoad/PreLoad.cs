using FleaSimulator.Overrides.Generators;
using FleaSimulator.Overrides.Servers;
using FleaSimulator.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;

namespace FleaSimulator.OnLoad;

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader)]
public class PreLoad(PresetService presetService, 
    ConfigServer configServer) : IOnLoad
{
    private readonly List<AbstractPatch> _patches =
    [
        new UpdateOverride(),
        new GenerateDynamicOffersOverride() 
    ];
    
    public async Task OnLoad()
    {
        await presetService.OnLoad();
        
        RagfairConfig ragfairConfig = configServer.GetConfig<RagfairConfig>();

        ragfairConfig.Dynamic.GenerateBaseFleaPrices.UseHandbookPrice = false;
        
        //completely override refresh system
        ragfairConfig.RunIntervalSeconds = (int)Math.Round(presetService.Config.Core.UpdateInterval * 60);
        ragfairConfig.Dynamic.ExpiredOfferThreshold = 0;
        
        //enable overrides
        foreach (AbstractPatch patch in _patches)
            patch.Enable();
        
    }
}