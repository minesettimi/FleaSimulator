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
    ConfigServer configServer) : IOnLoad
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
        
        RagfairConfig ragfairConfig = configServer.GetConfig<RagfairConfig>();

        ragfairConfig.Dynamic.GenerateBaseFleaPrices.UseHandbookPrice = false;
        
        //completely override refresh system
        ragfairConfig.Dynamic.ExpiredOfferThreshold = 0;

        //disable unreasonable price caps
        if (presetService.Config.Core.UnreasonablePrices)
        {
            foreach (KeyValuePair<MongoId, UnreasonableModPrices> modPrice in ragfairConfig.Dynamic.UnreasonableModPrices)
            {
                modPrice.Value.Enabled = false;
            }
        }
        
        //enable overrides
        foreach (AbstractPatch patch in _patches)
            patch.Enable();
        
    }
}