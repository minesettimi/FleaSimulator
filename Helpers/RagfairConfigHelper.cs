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

    public void InitializeTieredList()
    {
        ProgressiveConfig progConfig = presetService.Config.Core.ProgressiveFlea;
        Globals globalConfig = dbService.GetGlobals();
        
        ragfairConfig.TieredFlea.Enabled = true;

        if (progConfig.FullUnlockSystem)
        {
            globalConfig.Configuration.RagFair.MinUserLevel = 1;
        }
        
        ProgressiveItemConfig itemConfig = presetService.Config.ProgressiveItem;
        TieredFlea tieredFlea = ragfairConfig.TieredFlea;
        
        //clear tiered configs
        tieredFlea.AmmoTplUnlocks?.Clear();
        tieredFlea.UnlocksTpl.Clear();
        tieredFlea.UnlocksType.Clear();

        foreach ((MongoId key, int value) in itemConfig.Individual)
        {
            int trueValue = value;
            if (value <= 0)
                trueValue = progConfig.DefaultLevel;
            
            tieredFlea.UnlocksTpl.Add(key, trueValue);
        }
        
        foreach ((MongoId key, int value) in itemConfig.Parents)
        {
            int trueValue = value;
            if (value <= 0)
                trueValue = progConfig.DefaultLevel;
            
            tieredFlea.UnlocksType.Add(key, trueValue);
        }
        
        //set default by adding root item to types
        if (progConfig.FullUnlockSystem)
            tieredFlea.UnlocksType.Add("54009119af1c881c07000029", progConfig.DefaultLevel);
    }
}