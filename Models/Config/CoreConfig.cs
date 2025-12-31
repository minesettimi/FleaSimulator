using System.Text.Json.Serialization;

namespace FleaSimulator.Models.Config;

public class CoreConfig
{
    [JsonPropertyName("name")] public string Name { get; set; } = "Default Config";
    [JsonPropertyName("author")] public string Author { get; set; } = "minesettimi";
    [JsonPropertyName("buyConfig")] public BuyConfig BuyConfig { get; set; } = new();
    [JsonPropertyName("sellConfig")] public SellConfig SellConfig { get; set; } = new();
    [JsonPropertyName("wipePrices")] public WipePriceConfig WipePrices { get; set; } = new();
    [JsonPropertyName("updateInterval")] public double UpdateInterval { get; set; } = 60.0;
    [JsonPropertyName("simInterval")] public double SimulationInterval { get; set; } = 60.0;
    [JsonPropertyName("overrideUnreasonablePrices")] public bool UnreasonablePrices { get; set; } = true;
    [JsonPropertyName("debug")] public bool Debug { get; set; } = false;
    [JsonPropertyName("debugSimulation")] public bool DebugSimulation { get; set; } = false;
    [JsonPropertyName("debugItem")] public string DebugItem { get; set; } = "";
}

public class BuyConfig
{
    [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("canHaveNoOffers")] public bool CanHaveNoOffers { get; set; } = true;
    [JsonPropertyName("supplyMinOffers")] public int SupplyMinOffers { get; set; } = 2;
    [JsonPropertyName("supplyMaxOffers")] public int SupplyMaxOffers { get; set; } = 20;
    [JsonPropertyName("supplyOfferOffset")] public int SupplyOfferOffset { get; set; } = 1;
    [JsonPropertyName("supplyMinQuantity")] public int SupplyMinQuantity { get; set; } = 1;
    [JsonPropertyName("supplyMaxQuantity")] public int SupplyMaxQuantity { get; set; } = 5;
    //[JsonPropertyName("demandMinSoldChance")] public double DemandMinSoldChance { get; set; } = 0.0;
    //[JsonPropertyName("demandMaxSoldChance")] public double DemandMaxSoldChance { get; set; } = 0.85;
}

public class SellConfig
{
    [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("supplyMinPosition")] public int SupplyMinPosition { get; set; } = 2;
    [JsonPropertyName("supplyMaxPosition")] public int SupplyMaxPosition { get; set; } = 10;
    [JsonPropertyName("demandMinBuyChance")] public double DemandMinBuyChance { get; set; } = 10.0;
    [JsonPropertyName("demandMaxBuyChance")] public double DemandMaxBuyChance { get; set; } = 100.0;
    [JsonPropertyName("demandMinDelay")] public double DemandMinDelay { get; set; } = 3600.0;
    [JsonPropertyName("demandMaxDelay")] public double DemandMaxDelay { get; set; } = 0.0;
}

public class WipePriceConfig
{
    [JsonPropertyName("enabled")] public bool Enabled { get; set; }
    [JsonPropertyName("startLength")] public double StartLength { get; set; }
    [JsonPropertyName("earlyQuantityMult")] public double EarlyQuantityMult { get; set; } = 0.5;
}