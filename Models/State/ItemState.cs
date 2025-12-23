using System.Text.Json.Serialization;
using fleasimulator.Models.Config;

namespace FleaSimulator.Models.State;

public class ItemState
{
    [JsonIgnore] //this value must always be set anyways incase of preset changes
    public CategoryConfig Category { get; set; }
    public int TruePrice { get; set; } //the price that we try to stick to
    public int CurrentPrice { get; set; } //current item price
    public int TargetPrice { get; set; } //price that item is moving towards for next simulation
}