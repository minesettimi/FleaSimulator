using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;

namespace FleaSimulator.Models.Config;

public class ProgressiveItemConfig
{
    [JsonPropertyName("parents")] public Dictionary<MongoId, int> Parents { get; set; } = new();
    [JsonPropertyName("individual")] public Dictionary<MongoId, int> Individual { get; set; } = new();
}

public class ProgressiveConfig
{
    [JsonPropertyName("enabled")] public bool Enabled { get; set; } = false;
    [JsonPropertyName("fullUnlockSystem")] public bool FullUnlockSystem { get; set; } = false;
    [JsonPropertyName("defaultLevel")] public int DefaultLevel { get; set; } = 15;
}