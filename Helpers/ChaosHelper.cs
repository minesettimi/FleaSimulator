using fleasimulator.Models.Config;
using FleaSimulator.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Utils;

namespace FleaSimulator.Helpers;

[Injectable]
public class ChaosHelper(RandomUtil rng, MathUtil mathUtil)
{
    //randomly adjust value according to the provided configuration
    public double ChaosShift(double value, double chance, double minOffset, double maxOffset)
    {
        if (!rng.GetChance100(chance * 100))
            return value;
        
        //don't want constants
        double trueAdjust = rng.RandNum(minOffset, maxOffset);
        
        return rng.ReduceValueByPercent(value, trueAdjust * 100);
    }

    public double ChaosShift(CategoryConfig config, double value)
    {
        return ChaosShift(value, config.ChaosChance, config.ChaosMinOffset, config.ChaosMaxOffset);
    }

    public int ChaosShift(CategoryConfig config, int value)
    {
        return (int)Math.Round(ChaosShift(config, (double)value));
    }

    //does MapToRange but cleans up the code by always doing it for 0 to 1
    public double MapToRange01(double value, double outMin, double outMax)
    {
        return mathUtil.MapToRange(value, 0.01d, 1d, outMin, outMax);
    }
}