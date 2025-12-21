using FleaSimulator.Helpers;
using fleasimulator.Models.Config;
using FleaSimulator.Models.State;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using Path = System.IO.Path;

namespace FleaSimulator.Services;

[Injectable(InjectionType.Singleton)]
public class ItemDataService
    (PresetService preset, 
        JsonUtil jsonUtil, 
        ISptLogger<ItemDataService> logger,
        DatabaseService databaseService,
        SimItemHelper simHelper,
        ItemHelper itemHelper)
{
    public SaveState CurrentState;
    
    public async Task OnLoad()
    {
        SaveState? loadedState = await jsonUtil.DeserializeFromFileAsync<SaveState>(Path.Join(preset.ModPath, "state.json"));

        if (loadedState == null)
        {
            logger.Info("[FleaSimulator] No state file found, creating new market state.");
            loadedState = GenerateState();
        }
        else
            logger.Success("[FleaSimulator] State file successfully loaded.");

        CurrentState = loadedState;
        
        SaveCurrentState();
    }

    //create a new blank state based on the config
    private SaveState GenerateState()
    {
        SaveState newState = new();
        
        Dictionary<MongoId, TemplateItem> items = databaseService.GetItems();
        HandbookBase handbook = databaseService.GetHandbook();
        
        WipePriceConfig wipePriceConfig = preset.Config.Core.WipePrices;
        newState.WipeState = wipePriceConfig.Enabled ? WipeState.Start : WipeState.Middle;
        
        logger.Info($"[FleaSimulator] Generating market at state: {newState.WipeState}, {wipePriceConfig.Enabled}");

        foreach ((MongoId key, TemplateItem item) in items)
        {
            if (item.Properties is null || !item.Properties.CanSellOnRagfair.GetValueOrDefault(false)
                || !itemHelper.IsValidItem(item))
                continue;
            
            CategoryConfig category = simHelper.RetrieveItemCategory(item);
            
            ItemState itemState = new();
            double originalValue = handbook.Items.SingleOrDefault(i => i.Id == key)?.Price ?? 0;
            int convertedValue = Convert.ToInt32(Math.Round(originalValue * category.ValueMult));

            double earlyWipeMult = 1d;

            if (wipePriceConfig.Enabled)
                earlyWipeMult = category.EarlyWipeMult;
            
            itemState.Category = category;
            itemState.TruePrice = convertedValue;
            itemState.CurrentPrice = Convert.ToInt32(Math.Round(convertedValue * earlyWipeMult));
            itemState.TargetPrice = convertedValue;
            
            newState.Items.Add(key, itemState);
        }
        
        logger.Success("[FleaSimulator] New market state successfully generated.");

        return newState;
    }

    public void SaveCurrentState()
    {
        File.WriteAllTextAsync(Path.Join(preset.ModPath, "state.json"), 
            jsonUtil.Serialize(CurrentState, preset.Config.Core.Debug));
        
        logger.Debug("[FleaSimulator] Saved current market state.");
    }
}