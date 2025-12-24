using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;

namespace fleasimulator.Models.Config;

public class ItemConfig
{
    [JsonPropertyName("parents")] public Dictionary<MongoId, string> Parents { get; set; } = new();
    [JsonPropertyName("individual")] public Dictionary<MongoId, string> Individual { get; set; } = new();
}

public class CategoryConfig
{
    [JsonPropertyName("valueMult")] public double ValueMult { get; set; }
    [JsonPropertyName("earlyWipeMult")] public double EarlyWipeMult { get; set; }
    [JsonPropertyName("chaos")] public double Chaos { get; set; }
    [JsonPropertyName("chaosMinVal")] public double ChaosMinVal { get; set; }
    [JsonPropertyName("chaosMaxVal")] public double ChaosMaxVal { get; set; }
    [JsonPropertyName("chaosMaxIter")] public int ChaosMaxIterations { get; set; }
    [JsonPropertyName("chaosMinOffset")] public double ChaosMinOffset { get; set; }
    [JsonPropertyName("chaosMaxOffset")] public double ChaosMaxOffset { get; set; }
    [JsonPropertyName("chaosChance")] public double ChaosChance { get; set; }
    [JsonPropertyName("settleSpeed")] public double SettleSpeed { get; set; }
    [JsonPropertyName("minOfferPrice")] public double MinOfferPrice { get; set; }
    [JsonPropertyName("maxOfferPrice")] public double MaxOfferPrice { get; set; }
    [JsonPropertyName("nonUniformPrices")] public bool NonUniformPrices { get; set; }
    [JsonPropertyName("demand")] public double Demand { get; set; }
    [JsonPropertyName("supply")] public double Supply { get; set; }
    [JsonPropertyName("simChance")] public double SimChance { get; set; }

    //the default should be based on the configured default, write true defaults here
    public static CategoryConfig GenerateDefault()
    {
        CategoryConfig defaultConfig = new()
        {
            ValueMult = 1.0,
            EarlyWipeMult = 5.0,
            Chaos = 0.2,
            ChaosMinVal = 0.8,
            ChaosMaxVal = 1.5,
            ChaosMaxIterations = 3,
            ChaosMinOffset = 0.05,
            ChaosMaxOffset = 0.5,
            ChaosChance = 0.85,
            SettleSpeed = 0.2,
            MinOfferPrice = 0.95,
            MaxOfferPrice = 1.1,
            NonUniformPrices = true,
            Demand = 0.5,
            Supply = 0.5,
            SimChance = 1.0
        };

        return defaultConfig;
    }
}
