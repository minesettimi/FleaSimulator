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
    public WipeState WipeState { get; set; } = WipeState.Start;
    public DateTime StartDate { get; set; } = DateTime.Now;
    public long LastUpdate { get; set; } = 0;
    public long NextUpdate { get; set; } = 0;
    public Dictionary<MongoId, ItemState> Items { get; set; } = new();
}