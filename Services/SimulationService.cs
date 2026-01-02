using System.Diagnostics;
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
        //run simulations to catch up since last time
        if (preset.Config.Core.InterpSimulation && itemData.InterpSimulations > 0)
        {
            Stopwatch stopwatch = new();
            
            logger.Info($"[FleaSimulator] Running {itemData.InterpSimulations} missed simulations.");
            stopwatch.Start();
            
            for (int i = 0; i < itemData.InterpSimulations; i++)
            {
                SimulateEveryItem();
            }
            
            stopwatch.Stop();
            logger.Info($"[FleaSimulator] Finished catching up at {stopwatch.ElapsedMilliseconds}ms.");
        }
        
        //calculate when first simulation should occur
        long updateLeft = itemData.CurrentState.NextUpdate - itemData.CurrentState.UpdateTime;
        
        SimulationTimer = new Timer(_ =>
        {
            SimulateMarket();
        }, null, TimeSpan.FromSeconds(updateLeft), TimeSpan.FromMinutes(preset.Config.Core.SimulationInterval));
        
        logger.Debug($"[FleaSimulator] Starting timer in {updateLeft}s with a {(long)Math.Round(preset.Config.Core.SimulationInterval * 60)}s interval");
        
        return Task.CompletedTask;
    }

    public void SimulateMarket()
    {
        Stopwatch stopwatch = new();
        
        stopwatch.Start();
        
        SaveState state = itemData.CurrentState;

        if (state.WipeState == WipeState.Start && DateTime.Now > state.WipeChange)
        {
            logger.Info("[FleaSimulator] Early wipe has ended.");
            state.WipeState = WipeState.Middle;
        }
        
        logger.Debug("[FleaSimulator] Starting simulation.");

        if (state.NextUpdate > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            return;
        
        SimulateEveryItem();
        
        state.LastUpdate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        state.UpdateTime = state.LastUpdate;
        state.NextUpdate = state.LastUpdate + (long)Math.Round(preset.Config.Core.SimulationInterval * 60);
        
        itemData.SaveCurrentState();
        
        stopwatch.Stop();
        
        logger.Info($"[FleaSimulator] Finished simulation in {stopwatch.ElapsedMilliseconds}ms.");
    }

    public void SimulateEveryItem()
    {
        SaveState state = itemData.CurrentState;
        
        foreach ((MongoId key, ItemState item) in state.Items)
        {
            SimulateItem(key, item);
        }
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
        WipeState currentState = itemData.CurrentState.WipeState;

        if (currentState == WipeState.Middle || (time is not null && itemData.CurrentState.WipeChange < time))
        {
            if (trueValueDif < 0)
                settleVal = trueValueDif * chaosHelper.ChaosShift(category, category.SettleSpeedBelow);
            else
                settleVal = trueValueDif * chaosHelper.ChaosShift(category, category.SettleSpeed);
        }
        else
        {
            //calculate early wipe multiplier
            double diffPercent = item.CurrentPrice / (double)item.TruePrice;
            double iterationsLeft = (itemData.CurrentState.WipeChange - (time ?? DateTime.Now)).TotalMinutes;
            iterationsLeft /= preset.Config.Core.SimulationInterval;
            iterationsLeft = Math.Ceiling(iterationsLeft);

            double iterPercentage = 1 / iterationsLeft;
            
            double changeFactor = Math.Pow(diffPercent, iterPercentage) - 1;

            settleVal = trueValueDif * Math.Abs(changeFactor);

            if (preset.Config.Core.Debug && id == preset.Config.Core.DebugItem)
            {
                logger.Debug($"[Flea Simulator] Processed debug item, settled by {changeFactor}");
            }
        }
            
        item.TargetPrice -= (int)Math.Round(settleVal);

        bool balancedPricing = preset.Config.Core.WipePrices.BalancedPricing;
        
        //cap out prices based on configuration
        if (preset.Config.Core.BuyConfig.TraderPrices && (balancedPricing || currentState == WipeState.Middle))
        {
            double tradePrice = traderHelper.GetHighestSellToTraderPrice(id);
            if (tradePrice > item.TargetPrice)
                item.TargetPrice = (int)Math.Round(tradePrice);
        }

        if ((!balancedPricing || currentState == WipeState.Middle) && item.TargetPrice > category.MaxValue * item.TruePrice)
        {
            item.TargetPrice = (int)Math.Round(category.MaxValue * item.TruePrice);
        }

        double chaosClamp = category.ChaosMaxIterations - category.ChaosMinIterations;

        double shift = Math.Clamp(category.ChaosMaxIterations - category.Chaos - category.ChaosMinIterations, 0, chaosClamp);

        double chaosChance = randomUtil.GetBiasedRandomNumber(category.ChaosMinIterations, category.ChaosMaxIterations, 
            shift, 2d + category.Chaos*2);

        int chaosCount = (int)Math.Round(chaosChance);

        item.TargetPrice = Math.Max(item.TargetPrice, 1);
        
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