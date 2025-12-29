using FleaSimulator.Models.State;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace FleaSimulator.Services;

[Injectable(InjectionType.Singleton)]
public class PriceService(DatabaseService database,
    ItemDataService dataService,
    MathUtil mathUtil,
    ISptLogger<PriceService> logger
    )
{
    public void UpdatePrices()
    {
        logger.Info("[FleaSimulator] Updating offer prices.");

        Dictionary<MongoId, double> priceTable = database.GetPrices();
        
        SaveState currentState = dataService.CurrentState;
        
        //update state
        currentState.UpdateTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        dataService.SaveCurrentState();

        //update all prices
        Dictionary<MongoId, ItemState> itemStates = dataService.CurrentState.Items;
        foreach (KeyValuePair<MongoId, ItemState> states in itemStates)
        {
            double minPrice = Math.Min(states.Value.CurrentPrice, states.Value.TargetPrice);
            double maxPrice = Math.Max(states.Value.CurrentPrice, states.Value.TargetPrice);
            
            priceTable[states.Key] = Math.Round(mathUtil.MapToRange(currentState.UpdateTime, currentState.LastUpdate,
                currentState.NextUpdate,minPrice, maxPrice));
        }
    }
}