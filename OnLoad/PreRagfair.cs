using FleaSimulator.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace FleaSimulator.OnLoad;

[Injectable(TypePriority = OnLoadOrder.RagfairCallbacks - 3)]
public class PreRagfair(PresetService presetService,
    SimulationService simService,
    DebugService debug,
    ItemDataService itemService
    ): IOnLoad
{
    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        await itemService.OnLoad();
        
        if (presetService.Config.Core.Simulate)
            await simService.OnLoad();
        
        if (presetService.Config.Core.DebugSimulation)
            await debug.OnLoad();
    }
}