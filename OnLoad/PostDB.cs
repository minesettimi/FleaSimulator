using FleaSimulator.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;

namespace FleaSimulator.OnLoad;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class PostDb(ItemDataService dataService, SimulationService simService): IOnLoad
{
    public async Task OnLoad()
    {
        await dataService.OnLoad();
        await simService.OnLoad();
    }
}