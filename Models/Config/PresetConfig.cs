using System.Text.Json.Serialization;

namespace FleaSimulator.Models.Config;

public class LoaderConfig
{
    [JsonPropertyName("preset")] public string Preset { get; set; } = "default";
}

public class PresetConfig
{
    [JsonPropertyName("core")] public CoreConfig Core { get; set; } = new();
    [JsonPropertyName("items")] public ItemConfig Items { get; set; } = new();
    [JsonPropertyName("progressive")] public ProgressiveItemConfig ProgressiveItem { get; set; } = new();
    [JsonPropertyName("categories")] public Dictionary<string, SavedCategoryConfig> SavedCategories { get; set; } = new();
    
    [JsonIgnore] public Dictionary<string, CategoryConfig> Categories { get; set; } = new();
}