using FleaSimulator.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;

namespace FleaSimulator.OnLoad;

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader)]
public class PreLoad(PresetService presetService, 
    ConfigServer configServer) : IOnLoad
{
    public async Task OnLoad()
    {
        
        RagfairConfig ragfairConfig = configServer.GetConfig<RagfairConfig>();

        ragfairConfig.Dynamic.GenerateBaseFleaPrices.UseHandbookPrice = false;
        
        await presetService.OnLoad();
    }
}