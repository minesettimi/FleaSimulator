using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;

namespace FleaSimulator.Models.State;

public enum WipeState
{
    Start = 0,
    Middle
}

public class SaveState
{
    [JsonPropertyName("wipeState")] public WipeState WipeState { get; set; } = WipeState.Start;
    [JsonPropertyName("wipeChange")] public DateTime WipeChange { get; set; } = DateTime.Now; //when the wipe changes
    [JsonPropertyName("lastUpdate")] public long LastUpdate { get; set; } //last update in unix
    [JsonPropertyName("updateTime")]  public long UpdateTime {get; set;} // current position between last and next update, in unix
    [JsonPropertyName("nextUpdate")] public long NextUpdate { get; set; } //next update in unix
    [JsonPropertyName("items")] public Dictionary<MongoId, ItemState> Items { get; set; } = new();
}