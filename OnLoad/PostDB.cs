using FleaSimulator.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace FleaSimulator.OnLoad;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class PostDb(PresetService presetService,
    ItemDataService dataService,
    SimulationService simService,
    DebugService debug
    ): IOnLoad
{
    public async Task OnLoad()
    {
        await dataService.OnLoad();
        await simService.OnLoad();
        
        if (presetService.Config.Core.DebugSimulation)
            await debug.OnLoad();
    }
}