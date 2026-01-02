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
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;

namespace FleaSimulator.OnLoad;

[Injectable(TypePriority = OnLoadOrder.PreSptModLoader)]
public class PreLoad(PresetService presetService,
    RagfairConfigHelper configHelper) : IOnLoad
{
    private readonly List<AbstractPatch> _patches =
    [
        new UpdateOverride(),
        new CreateOffersFromAssortOverride(),
        new GenerateDynamicOffersOverride(),
        new GetDynamicPriceOverride(),
        new CalculateStackCountOverride(),
        new CreatePackOfferOverride(),
        new CreateMultiOfferOverride(),
        new CreateSingleOfferOverride()
    ];
    
    public async Task OnLoad()
    {
        await presetService.OnLoad();
        await configHelper.OnLoad();
        
        //enable overrides
        foreach (AbstractPatch patch in _patches)
            patch.Enable();
        
    }
}