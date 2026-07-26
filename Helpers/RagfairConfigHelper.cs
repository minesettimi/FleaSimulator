using FleaSimulator.Models.Config;
using FleaSimulator.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;

namespace FleaSimulator.Helpers;

[Injectable]
public class RagfairConfigHelper(ConfigServer configServer,
    DatabaseService dbService,
    PresetService presetService)
{
    private readonly RagfairConfig ragfairConfig = configServer.GetConfig<RagfairConfig>();

    public Task OnLoad()
    {
        ragfairConfig.Dynamic.GenerateBaseFleaPrices.UseHandbookPrice = false;
        
        //override refresh system
        ragfairConfig.Dynamic.ExpiredOfferThreshold = 0;

        if (!presetService.Config.Core.BuyConfig.EnableBarter)
            ragfairConfig.Dynamic.Barter.ChancePercent = 0.0;
            
        //disable unreasonable price caps
        if (presetService.Config.Core.UnreasonablePrices)
        {
            foreach (KeyValuePair<MongoId, UnreasonableModPrices> modPrice in ragfairConfig.Dynamic.UnreasonableModPrices)
            {
                modPrice.Value.Enabled = false;
            }
        }
        
        return Task.CompletedTask;
    }
}