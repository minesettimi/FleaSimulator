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
    public DateTime StartDate { get; set; } = DateTime.Now; //market creation date
    public long LastUpdate { get; set; } //last update in unix
    public long UpdateTime {get; set;} // current position between last and next update, in unix
    public long NextUpdate { get; set; } //next update in unix
    public Dictionary<MongoId, ItemState> Items { get; set; } = new();
}