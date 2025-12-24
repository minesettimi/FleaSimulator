using FleaSimulator.Helpers;
using fleasimulator.Models.Config;
using FleaSimulator.Models.State;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace FleaSimulator.Services;

[Injectable(InjectionType.Singleton)]
public class SimulationService(PresetService preset,
    ItemDataService itemData,
    ChaosHelper chaosHelper,
    RandomUtil randomUtil,
    ISptLogger<SimulationService> logger)
{
    public Timer SimulationTimer;
    
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
        
        logger.Debug("[FleaSimulator] Starting simulation.");

        if (state.NextUpdate > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            return;
        
        foreach ((MongoId key, ItemState item) in state.Items)
        {
            CategoryConfig category = item.Category;

            item.CurrentPrice = item.TargetPrice;

            if (!randomUtil.GetChance100(category.SimChance * 100))
                continue;
            
            //the value will slowly progress towards the true value
            int trueValueDif = item.CurrentPrice - item.TruePrice;
            double settleVal = randomUtil.ReduceValueByPercent(trueValueDif, 
                chaosHelper.ChaosShift(category, category.SettleSpeed));
            
            item.TargetPrice -= (int)Math.Round(settleVal);

            double chaosChance = randomUtil.GetBiasedRandomNumber(1d, category.ChaosMaxIterations, 
                category.ChaosMaxIterations - category.Chaos, 2d);

            int chaosCount = (int)Math.Round(chaosChance);

            for (int i = 0; i < chaosCount; i++)
            {
                item.TargetPrice = chaosHelper.ChaosShift(category, item.TargetPrice);
            }

        }
        
        state.LastUpdate = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        state.UpdateTime = state.LastUpdate;
        state.NextUpdate = state.LastUpdate + (long)Math.Round(preset.Config.Core.UpdateInterp * 60);
        
        itemData.SaveCurrentState();
        
        long endTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        logger.Info($"[FleaSimulator] Finished simulation in {endTime - startTime}ms.");
    }
}