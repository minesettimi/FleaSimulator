using SPTarkov.DI.Annotations;

namespace FleaSimulator.Utils;

[Injectable(InjectionType.Singleton)]
public class FleaMathUtil
{
    //functions like MapToRange but the values use an exponential curve
    public double MapToRangeExp(double x, double minIn, double maxIn, double minOut, double maxOut, int exp)
    {
        double difIn = maxIn - minIn;
        double difOut = maxOut - maxIn;

        double scale = (x - minIn) / difIn;

        scale = Math.Pow(scale, exp);
        
        return Math.Clamp(minOut + scale * difOut, minOut, maxOut);
    }

    public double MapToRangeExp(double x, double minOut, double maxOut, int exp)
    {
        return MapToRangeExp(x, 0, 1, minOut, maxOut, exp);
    }
}