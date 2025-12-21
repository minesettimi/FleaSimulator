using SPTarkov.DI.Annotations;

namespace FleaSimulator.Services;

[Injectable(InjectionType.Singleton)]
public class SimulationService(PresetService presetService)
{
    public Task OnLoad()
    {
        
        
        return Task.CompletedTask;
    }
}