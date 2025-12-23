using System.Reflection;
using FleaSimulator.Models;
using fleasimulator.Models.Config;
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
        loader = await jsonUtil.DeserializeFromFileAsync<LoaderConfig>(Path.Join(ModPath, "loader.json")) ?? new LoaderConfig();
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
        SaveConfig();
    }

    private void ValidateConfig()
    {
        CoreConfig coreConfig = Config.Core;
        
        if (coreConfig.UpdateInterval > coreConfig.UpdateInterp)
        {
            logger.Warning("[FleaSimulator] Update interval greater than the update interp! Clamping.");
            coreConfig.UpdateInterval = coreConfig.UpdateInterp;
        }

        if (!Config.Categories.ContainsKey("Default"))
        {
            logger.Error("[FleaSimulator] Default category not specified in config! Generating new one.");
            Config.Categories["Default"] = DefaultCategoryConfig;
        }

        if (coreConfig.WipePrices is { Enabled: true, StartLength: <= 0 })
        {
            logger.Warning("[FleaSimulator] Wipe prices enabled but start length is not valid! Disabling.");
            coreConfig.WipePrices.Enabled = false;
        }

        //TODO: ADD MORE VALIDATIONS
    }

    private void SaveConfig()
    {
        string presetPath = Path.Join(ModPath, "Presets");
        if (!Directory.Exists(presetPath))
        {
            Directory.CreateDirectory(presetPath);
        }
        
        File.WriteAllTextAsync(Path.Join(ModPath, "loader.json"), jsonUtil.Serialize(loader));
        File.WriteAllTextAsync(Path.Join(presetPath, $"{CurrentPreset}.jsonc"), jsonUtil.Serialize(Config));
    }
    
    //assign all null category values the values 
    private void AssignCategoryDefaults()
    {
        PropertyInfo[] categoryProps = typeof(CategoryConfig).GetProperties();
        
        foreach (KeyValuePair<string, CategoryConfig> category in Config.Categories)
        {
            //loop through each value of CategoryConfig and set the values to the default if null
            foreach (PropertyInfo prop in categoryProps)
            {
                if (prop.GetValue(category.Value) is not null) continue;
                
                prop.SetValue(category.Value, DefaultCategoryConfig);
                logger.Debug($"Assigning category var {category.Key} to default {category.Value}");
            }
        }
    }
}