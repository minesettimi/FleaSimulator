using System.Reflection;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;

namespace FleaSimulator.Models.Config;

public class ItemConfig
{
    [JsonPropertyName("parents")] public Dictionary<MongoId, string> Parents { get; set; } = new();
    [JsonPropertyName("individual")] public Dictionary<MongoId, string> Individual { get; set; } = new();
}

public class CategoryConfig
{
    public double ValueMult { get; set; }
    public double EarlyWipeMult { get; set; }
    public double Chaos { get; set; }
    public double ChaosMinVal { get; set; }
    public double ChaosMaxVal { get; set; }
    public double ChaosMinIterations { get; set; }
    public int ChaosMaxIterations { get; set; }
    public double ChaosMinOffset { get; set; }
    public double ChaosMaxOffset { get; set; }
    public double ChaosChance { get; set; }
    public double SettleSpeed { get; set; }
    public bool  SettleOnlyMin { get; set; }
    public double MinOfferPrice { get; set; }
    public double MaxOfferPrice { get; set; }
    public double Demand { get; set; }
    public double Supply { get; set; }
    public double SimChance { get; set; }

    //the default should be based on the configured default, write true defaults here
    public static CategoryConfig GenerateDefault()
    {
        CategoryConfig defaultConfig = new()
        {
            ValueMult = 1.0,
            EarlyWipeMult = 5.0,
            Chaos = 0.2,
            ChaosMinVal = 0.9,
            ChaosMaxVal = 1.1,
            ChaosMinIterations = 0,
            ChaosMaxIterations = 3,
            ChaosMinOffset = -0.05,
            ChaosMaxOffset = 0.05,
            ChaosChance = 0.75,
            SettleSpeed = 0.01,
            SettleOnlyMin = false,
            MinOfferPrice = 0.95,
            MaxOfferPrice = 1.1,
            Demand = 0.5,
            Supply = 0.4,
            SimChance = 1.0
        };

        return defaultConfig;
    }

    public static CategoryConfig CopyValues(CategoryConfig defaultCategory, SavedCategoryConfig source)
    {
        CategoryConfig newCategory = new();
        
        foreach (PropertyInfo prop in typeof(SavedCategoryConfig).GetProperties())
        {

            if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                continue;
                
            PropertyInfo? otherProp = typeof(CategoryConfig).GetProperty(prop.Name);

            if (otherProp == null)
                continue;
                
            if (prop.GetValue(source) is null)
            {
                otherProp.SetValue(newCategory, otherProp.GetValue(defaultCategory, null), null);
                continue;
            }
            
            otherProp.SetValue(newCategory, prop.GetValue(source, null), null);
        }

        return newCategory;
    }
}

//afaik theres not really a more efficient way of doing this without over-enginnering, so copy-paste nullables it is
public class SavedCategoryConfig
{
    [JsonPropertyName("valueMult")] public double? ValueMult { get; set; }
    [JsonPropertyName("earlyWipeMult")] public double? EarlyWipeMult { get; set; }
    [JsonPropertyName("chaos")] public double? Chaos { get; set; }
    [JsonPropertyName("chaosMinVal")] public double? ChaosMinVal { get; set; }
    [JsonPropertyName("chaosMaxVal")] public double? ChaosMaxVal { get; set; }
    [JsonPropertyName("chaosMinIter")] public double? ChaosMinIterations { get; set; }
    [JsonPropertyName("chaosMaxIter")] public int? ChaosMaxIterations { get; set; }
    [JsonPropertyName("chaosMinOffset")] public double? ChaosMinOffset { get; set; }
    [JsonPropertyName("chaosMaxOffset")] public double? ChaosMaxOffset { get; set; }
    [JsonPropertyName("chaosChance")] public double? ChaosChance { get; set; }
    [JsonPropertyName("settleSpeed")] public double? SettleSpeed { get; set; }
    [JsonPropertyName("settleOnlyMin")] public bool? SettleOnlyMin { get; set; }
    [JsonPropertyName("minOfferPrice")] public double? MinOfferPrice { get; set; }
    [JsonPropertyName("maxOfferPrice")] public double? MaxOfferPrice { get; set; }
    [JsonPropertyName("demand")] public double? Demand { get; set; }
    [JsonPropertyName("supply")] public double? Supply { get; set; }
    [JsonPropertyName("simChance")] public double? SimChance { get; set; }
}