using FleaSimulator.Models.Config;
using FleaSimulator.Models.State;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;
using Path = System.IO.Path;

namespace FleaSimulator.Services;

[Injectable(InjectionType.Singleton)]
public class ItemDataService
    (PresetService preset, 
        JsonUtil jsonUtil, 
        ISptLogger<ItemDataService> logger,
        DatabaseService databaseService,
        ItemHelper itemHelper,
        DatabaseService database,
        SaveServer saveServer,
        ICloner cloner)
{
    public SaveState CurrentState;
    public int InterpSimulations = 0;

    private Dictionary<MongoId, double> originalItems;

    public async Task OnLoad()
    {
        SaveState? loadedState = await jsonUtil.DeserializeFromFileAsync<SaveState>(Path.Join(preset.ModPath, "state.json"));

        Dictionary<MongoId, double> priceList = database.GetPrices();
        originalItems = cloner.Clone(priceList)!;

        if (loadedState == null)
        {
            logger.Info("[FleaSimulator] No state file found, creating new market state.");
            loadedState = GenerateState();
        }
        else
        {
            //update unix times to match the current time
            //this isn't 100% accurate as updatetime isn't constantly updated
            long current = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            //time between the last update and loading the data
            long difference = current - loadedState.UpdateTime;
            long nextTimeDiff = loadedState.NextUpdate - loadedState.UpdateTime;
            long interpDiff = difference - nextTimeDiff;
            
            int simTimeSecs = (int)Math.Round(preset.Config.Core.SimulationInterval * 60);
            
            
            if (preset.Config.Core.InterpSimulation)
            {
                if (interpDiff > 0)
                {
                    //add one since there was enough time since the last update to reach the next update
                    InterpSimulations = (int)Math.Floor(interpDiff / (double)simTimeSecs) + 1;
                    loadedState.NextUpdate = current + interpDiff % simTimeSecs; //set next update to whatever time is remaining
                }
                else
                {
                    loadedState.NextUpdate = current - interpDiff; //there isn't enough difference, set to whatever time is left
                }

                
                loadedState.LastUpdate = loadedState.NextUpdate - simTimeSecs;
            }
            else
            {
                //don't sim inbetween, shift times to pick up where we left off
                loadedState.LastUpdate += difference;
                loadedState.NextUpdate += difference;
            }
            
            loadedState.UpdateTime = current;
            
            MapCategories(loadedState);
            
            logger.Success($"[FleaSimulator] State file successfully loaded, current state is {loadedState.WipeState}.");
            
            if (loadedState.WipeState == WipeState.Start)
                logger.Info($"[FleaSimulator] Wipe start ends at {loadedState.WipeChange.ToLongDateString()}");
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
        
        logger.Info($"[FleaSimulator] Generating new market at state: {newState.WipeState}.");
        if (wipePriceConfig.DisableLevel != -1)
        {
            int maxLevel = 0;
            
            foreach ((MongoId session, SptProfile profile) in saveServer.GetProfiles())
            {
                if (saveServer.IsProfileInvalidOrUnloadable(session))
                    continue;

                int level = profile.CharacterData?.PmcData?.Info?.Level ?? 0;

                if (level > maxLevel)
                    maxLevel = level;
            }

            if (maxLevel >= wipePriceConfig.DisableLevel)
            {
                newState.WipeState = WipeState.Middle;
                logger.Info($"[FleaSimulator] Profile found with level {maxLevel}, force skipping early wipe.");
            }
        }
        
        if (newState.WipeState == WipeState.Start)
            newState.WipeChange = DateTime.Now.AddDays(wipePriceConfig.StartLength);

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
            double? liveValue = !preset.Config.Core.BuyConfig.UseHandbook ? originalItems.GetValueOrDefault(key) : null;

            if (liveValue == 0 || liveValue == null)
                liveValue = handbook.Items.SingleOrDefault(i => i.Id == key)?.Price;

            double resultVal = Math.Round(category.ValueMult * liveValue.GetValueOrDefault(0));
            
            int convertedValue = Convert.ToInt32(Math.Clamp(resultVal, int.MinValue + 1, int.MaxValue - 1));

            double earlyWipeMult = 1d;

            if (newState.WipeState == WipeState.Start)
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

    private void MapCategories(SaveState saveState)
    {
        Dictionary<MongoId, TemplateItem> items = databaseService.GetItems();
        
        foreach ((MongoId key, ItemState itemState) in saveState.Items)
        {
            items.TryGetValue(key, out TemplateItem? trueItem);

            if (trueItem is null)
            {
                logger.Error($"[FleaSimulator] Item with key {key} does not exist in-game, deleting.");
                saveState.Items.Remove(key);
                continue;
            }

            CategoryConfig? category = RetrieveItemCategory(trueItem);

            if (category is null)
            {
                logger.Warning($"[FleaSimulator] Item {trueItem.Name} found in state but is blacklisted, deleting.");
                saveState.Items.Remove(key);
                continue;
            }
            
            itemState.Category = category;
        }
    }

    public CategoryConfig? RetrieveItemCategory(TemplateItem item)
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
        else
        {
            foreach (MongoId parentKey in itemConfig.Parents.Keys)
            {
                if (!itemHelper.IsOfBaseclass(item.Id, parentKey)) continue;
            
                if (categoryName == "Blacklist")
                    return null;
                
                resultCategory = preset.Config.Categories.GetValueOrDefault(itemConfig.Parents[parentKey]);
                break;
            }
        }

        return resultCategory ?? preset.DefaultCategoryConfig;
    }
}