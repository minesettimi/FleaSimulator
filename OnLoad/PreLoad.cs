using FleaSimulator.Helpers;
using FleaSimulator.Overrides.Controllers;
using FleaSimulator.Overrides.Generators;
using FleaSimulator.Overrides.Helpers;
using FleaSimulator.Overrides.Servers;
using FleaSimulator.Overrides.Services;
using FleaSimulator.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Reflection.Patching;
using SPTarkov.Server.Core.DI;

namespace FleaSimulator.OnLoad;

[Injectable(TypePriority = OnLoadOrder.Preload + 20)]
public class PreLoad(PresetService presetService,
    RagfairConfigHelper configHelper,
    IEnumerable<IRuntimePatch> patches
    ) : IOnLoad
{
    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        await presetService.OnLoad();
        await configHelper.OnLoad();
        
        //enable overrides
        foreach (IRuntimePatch patch in patches)
            patch.Enable();
    }
}