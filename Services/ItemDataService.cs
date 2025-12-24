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
        {
            //update unix times to match the current time
            //this isn't 100% foolproof as updatetime isn't always updated
            long current = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            //time between the last update and loading the data
            long difference = current - loadedState.UpdateTime;
            
            //shift all times
            loadedState.LastUpdate += difference;
            loadedState.NextUpdate += difference;
            loadedState.UpdateTime = current;
            
            MapCategories();
            
            logger.Success("[FleaSimulator] State file successfully loaded.");
        }

        CurrentState = loadedState;
        
        SaveCurrentState();
    }


    public void SaveCurrentState()
    {
        File.WriteAllTextAsync(Path.Join(preset.ModPath, "state.json"), 
            jsonUtil.Serialize(CurrentState, preset.Config.Core.Debug));
        
        logger.Debug("[FleaSimulator] Saved current market state.");
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
            
            CategoryConfig? category = RetrieveItemCategory(item);

            //blacklisted item
            if (category is null)
                continue;
            
            ItemState itemState = new();
            double originalValue = handbook.Items.SingleOrDefault(i => i.Id == key)?.Price ?? 0;
            int convertedValue = Convert.ToInt32(Math.Round(originalValue * category.ValueMult));

            double earlyWipeMult = 1d;

            if (wipePriceConfig.Enabled)
                earlyWipeMult = category.EarlyWipeMult;

            int startingPrice = Convert.ToInt32(Math.Round(convertedValue * earlyWipeMult));
            
            itemState.Category = category;
            itemState.TruePrice = convertedValue;
            itemState.CurrentPrice = startingPrice;
            itemState.TargetPrice = startingPrice;
            
            newState.Items.Add(key, itemState);
        }
        
        logger.Success($"[FleaSimulator] Successfully generated new market with {newState.Items.Count} items.");

        return newState;
    }

    private void MapCategories()
    {
        Dictionary<MongoId, TemplateItem> items = databaseService.GetItems();
        
        foreach ((MongoId key, ItemState itemState) in CurrentState.Items)
        {
            items.TryGetValue(key, out TemplateItem? trueItem);

            if (trueItem is null)
            {
                logger.Error($"[FleaSimulator] Item with key {key} does not exist in-game, deleting.");
                CurrentState.Items.Remove(key);
                continue;
            }

            CategoryConfig? category = RetrieveItemCategory(trueItem);

            if (category is null)
            {
                logger.Warning($"[FleaSimulator] Item {trueItem.Name} found in state but is blacklisted, deleting.");
                CurrentState.Items.Remove(key);
                continue;
            }
            
            itemState.Category = category;
        }
    }
    
    private CategoryConfig? RetrieveItemCategory(TemplateItem item)
    {
        CategoryConfig? resultCategory = null;
        ItemConfig itemConfig = preset.Config.Items;

        //first try to get the item itself, otherwise try to get its parent
        if (itemConfig.Individual.TryGetValue(item.Id, out string? categoryName) 
            || itemConfig.Parents.TryGetValue(item.Parent, out categoryName))
        {
            if (categoryName == "Blacklist")
                return null;
            
            resultCategory = preset.Config.Categories.GetValueOrDefault(categoryName);
        }
        
        return resultCategory ?? preset.DefaultCategoryConfig;
    }
}