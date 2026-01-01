using System.Reflection;
using FleaSimulator.Models;
using FleaSimulator.Models.Config;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace FleaSimulator.Services;

[Injectable(InjectionType.Singleton)]
public class PresetService
    (ModHelper helper, 
        JsonUtil jsonUtil, 
        ISptLogger<PresetService> logger)
{
    public PresetConfig Config { get; private set; }
    public CategoryConfig DefaultCategoryConfig { get; private set; } = CategoryConfig.GenerateDefault();
    public string CurrentPreset { get; private set; } = "default";
    
    private LoaderConfig loader;

    public string ModPath => helper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

    public async Task OnLoad()
    {
        LoaderConfig? tempLoader = await jsonUtil.DeserializeFromFileAsync<LoaderConfig>(Path.Join(ModPath, "loader.json"));

        if (tempLoader == null)
        {
            tempLoader = new LoaderConfig();
            await File.WriteAllTextAsync(Path.Join(ModPath, "loader.json"), 
                jsonUtil.Serialize(tempLoader, true));
            loader = tempLoader;
        }
        
        CurrentPreset = loader.Preset;

        PresetConfig? config = await jsonUtil.DeserializeFromFileAsync<PresetConfig>(Path.Join(ModPath, "Presets", $"{CurrentPreset}.jsonc"));
        
        if (config == null)
        {
            logger.Info($"[FleaSimulator] No preset found with name: {CurrentPreset}. Attempting load default.");
            config = await jsonUtil.DeserializeFromFileAsync<PresetConfig>(Path.Join(ModPath, "Presets", "default.jsonc"));
            if (config == null)
            {
                logger.Error("[FleaSimulator] Default preset not found! Generating new one.");
                loader.Preset = "default";
                
                config = new PresetConfig();
            }
        }
        else
        {
            logger.Success($"[FleaSimulator] Loaded preset {config.Core.Name} created by {config.Core.Author}.");
        }

        Config = config;
        
        ValidateConfig();
        DefaultCategoryConfig = Config.Categories["Default"];
        
        AssignCategoryDefaults();
    }

    private void ValidateConfig()
    {
        CoreConfig coreConfig = Config.Core;
        
        if (coreConfig.UpdateInterval <= 0)
        {
            logger.Warning("[FleaSimulator] Update interval is too low! Setting to 1.");
            coreConfig.UpdateInterval = 1;
        }

        if (coreConfig.SimulationInterval <= 0)
        {
            logger.Warning("[FleaSimulator] Simulation interval is too low! Setting to 1.");
            coreConfig.SimulationInterval = 1;
        }

        if (!Config.SavedCategories.TryGetValue("Default", out SavedCategoryConfig? category))
        {
            logger.Error("[FleaSimulator] Default category not specified in config! Generating new one.");
            Config.Categories["Default"] = DefaultCategoryConfig;
        }
        else
        {
            Config.Categories["Default"] = CategoryConfig.CopyValues(DefaultCategoryConfig, category);
        }

        if (coreConfig.WipePrices is { Enabled: true, StartLength: <= 0 })
        {
            logger.Warning("[FleaSimulator] Wipe prices enabled but start length is not valid! Disabling.");
            coreConfig.WipePrices.Enabled = false;
        }

        //TODO: ADD MORE VALIDATIONS
    }
    
    //migrate all saved categories to regular categories
    //this bypasses having to null check EVERYWHERE that a category is used
    private void AssignCategoryDefaults()
    {
        foreach (KeyValuePair<string, SavedCategoryConfig> category in Config.SavedCategories)
        {
            if (category.Key == "Default")
                continue;
            
            Config.Categories.Add(category.Key, CategoryConfig.CopyValues(DefaultCategoryConfig, category.Value));
        }
    }
}