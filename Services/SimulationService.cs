using FleaSimulator.Helpers;
using FleaSimulator.Models.Config;
using FleaSimulator.Models.State;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;

namespace FleaSimulator.Services;

[Injectable(InjectionType.Singleton)]
public class SimulationService(PresetService preset,
    ItemDataService itemData,
    ChaosHelper chaosHelper,
    RandomUtil randomUtil,
    TraderHelper traderHelper,
    ConfigServer configServer,
    ISptLogger<SimulationService> logger)
{
    private readonly RagfairConfig _ragfairConfig = configServer.GetConfig<RagfairConfig>();
    private Timer SimulationTimer;
    
    public Task OnLoad()
    {   
        //calculate when first simulation should occur
        long updateLeft = itemData.CurrentState.NextUpdate - itemData.CurrentState.UpdateTime;
        
        SimulationTimer = new Timer(_ =>
        {
            SimulateMarket();
        }, null, TimeSpan.FromSeconds(updateLeft), TimeSpan.FromMinutes(preset.Config.Core.UpdateInterp));
        
        logger.Debug($"[FleaSimulator] Starting timer in {updateLeft}s with a {(long)Math.Round(preset.Config.Core.UpdateInterp * 60)}s interval");
        
        return Task.CompletedTask;
    }

    private void SimulateMarket()
    {
        long startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        SaveState state = itemData.CurrentState;

        if (state.WipeState == WipeState.Start && DateTime.Now > state.WipeChange)
        {
            logger.Info("[FleaSimulator] Wipe middle has begun.");
            state.WipeState = WipeState.Middle;
        }
        
        logger.Debug("[FleaSimulator] Starting simulation.");

        if (state.NextUpdate > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            return;
        
        foreach ((MongoId key, ItemState item) in state.Items)
        {
            SimulateItem(key, item);
        }
        
        state.LastUpdate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        state.UpdateTime = state.LastUpdate;
        state.NextUpdate = state.LastUpdate + (long)Math.Round(preset.Config.Core.UpdateInterp * 60);
        
        itemData.SaveCurrentState();
        
        long endTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        logger.Info($"[FleaSimulator] Finished simulation in {endTime - startTime}ms.");
    }

    public void SimulateItem(MongoId id, ItemState item, DateTime? time = null)
    {
        CategoryConfig category = item.Category;
        
        item.CurrentPrice = item.TargetPrice;

        if (!randomUtil.GetChance100(category.SimChance * 100))
            return;
        
        //the value will slowly progress towards the true value
        int trueValueDif = item.CurrentPrice - item.TruePrice;

        double settleVal = 0;

        if (itemData.CurrentState.WipeState == WipeState.Middle || (time is not null && itemData.CurrentState.WipeChange < time))
        {
            if (!category.SettleOnlyMin || trueValueDif < 0)
                settleVal = trueValueDif * chaosHelper.ChaosShift(category, category.SettleSpeed);
        }
        else
        {
            //calculate early wipe multiplier
            double diffPercent = item.CurrentPrice / (double)item.TruePrice;
            double iterationsLeft = (itemData.CurrentState.WipeChange - (time ?? DateTime.Now)).TotalMinutes;
            iterationsLeft /= preset.Config.Core.UpdateInterp;
            iterationsLeft = Math.Ceiling(iterationsLeft);

            double iterPercentage = 1 / iterationsLeft;
            
            double changeFactor = Math.Pow(diffPercent, iterPercentage) - 1;

            settleVal = trueValueDif * changeFactor;
        }
            
        item.TargetPrice -= (int)Math.Round(settleVal);
        
        //cap out prices based on configuration
        if (_ragfairConfig.Dynamic.UseTraderPriceForOffersIfHigher)
        {
            double tradePrice = traderHelper.GetHighestSellToTraderPrice(id);
            if (tradePrice > item.TargetPrice)
                item.TargetPrice = (int)Math.Round(tradePrice);
        }

        double chaosClamp = category.ChaosMaxIterations - 1d;

        double shift = Math.Clamp(category.ChaosMaxIterations - category.Chaos - category.ChaosMinIterations, category.ChaosMinIterations, chaosClamp);

        double chaosChance = randomUtil.GetBiasedRandomNumber(category.ChaosMinIterations, category.ChaosMaxIterations, 
            shift, 2d + category.Chaos*2);

        int chaosCount = (int)Math.Round(chaosChance);

        int previousPrice = item.TargetPrice;
        double minPrice = previousPrice * category.ChaosMinVal;
        double maxPrice = previousPrice * category.ChaosMaxVal;
        for (int i = 0; i < chaosCount; i++)
        {
            item.TargetPrice = chaosHelper.ChaosShift(category, item.TargetPrice);
        }

        item.TargetPrice = (int)Math.Round(Math.Clamp(item.TargetPrice, minPrice, maxPrice));
    }
}