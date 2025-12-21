using System.Text.Json.Serialization;
using fleasimulator.Models.Config;
using SPTarkov.Server.Core.Models.Common;

namespace FleaSimulator.Models;

public class LoaderConfig
{
    [JsonPropertyName("preset")] public string Preset { get; set; } = "default";
}

public class PresetConfig
{
    [JsonPropertyName("core")] public CoreConfig Core { get; set; } = new();
    [JsonPropertyName("items")] public ItemConfig Items { get; set; } = new();

    [JsonPropertyName("categories")] public Dictionary<string, CategoryConfig> Categories { get; set; } = new()
    {
        { "Default", new CategoryConfig() }
    };
}