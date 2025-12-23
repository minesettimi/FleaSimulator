using fleasimulator.Models.Config;
using FleaSimulator.Models.State;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;

namespace FleaSimulator.Services;

[Injectable(InjectionType.Singleton)]
public class SimulationService(PresetService preset,
    ItemDataService itemData,
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
            CategoryConfig itemCategory = item.Category;
        }
        
        long endTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        logger.Info($"[FleaSimulator] Finished simulation in {endTime - startTime}ms.");
    }
}