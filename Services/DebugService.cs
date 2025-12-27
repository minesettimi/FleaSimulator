using System.Text;
using FleaSimulator.Models.Config;
using FleaSimulator.Models.State;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using Path = System.IO.Path;

namespace FleaSimulator.Services;

[Injectable(InjectionType = InjectionType.Singleton)]
public class DebugService(PresetService preset,
    SimulationService simService,
    DatabaseService db,
    ItemDataService itemDataService,
    ModHelper modHelper,
    ISptLogger<DebugService> logger
    )
{
    //generate a item state and simulate it over the course of a long period of time to test how the value escalates
    public async Task OnLoad()
    {
        if (!preset.Config.Core.DebugSimulation || preset.Config.Core.DebugItem == "")
            return;

        Dictionary<MongoId, TemplateItem> items = db.GetItems();
        TemplateItem? debugItem = items.GetValueOrDefault(preset.Config.Core.DebugItem);

        if (debugItem == null)
        {
            logger.Warning("[FleaSimulator] Debug item not found, cancelling debug simulation.");
            return;
        }
        
        HandbookBase handbook = db.GetHandbook();
        ItemState testItem = new();
        
        CategoryConfig? category = itemDataService.RetrieveItemCategory(debugItem);

        //blacklisted item
        if (category is null)
        {
            logger.Warning("[FleaSimulator] Debug item is blacklisted, cancelling debug simulation.");
            return;
        }
        
        double originalValue = handbook.Items.SingleOrDefault(i => i.Id == preset.Config.Core.DebugItem)?.Price ?? 0;
        int convertedValue = Convert.ToInt32(Math.Round(originalValue * category.ValueMult));

        double earlyWipeMult = 1d;

        if (preset.Config.Core.WipePrices.Enabled)
            earlyWipeMult = category.EarlyWipeMult;

        int startingPrice = Convert.ToInt32(Math.Round(convertedValue * earlyWipeMult));
            
        testItem.Category = category;
        testItem.TruePrice = convertedValue;
        testItem.CurrentPrice = startingPrice;
        testItem.TargetPrice = startingPrice;

        double totalTime = TimeSpan.FromDays(30 * 3).TotalMinutes;
        totalTime /= preset.Config.Core.UpdateInterp;

        int iterationCount = (int)Math.Round(totalTime);
        
        //build csv export
        StringBuilder csv = new();
        csv.AppendLine("Test Date,True Price,Current Price,Target Price");
        
        DateTime testDate = DateTime.Now;

        for (int i = 0; i < iterationCount; i++)
        {
            csv.AppendLine($"{testDate.AddMinutes(preset.Config.Core.UpdateInterp)},{testItem.TruePrice}," +
                           $"{testItem.CurrentPrice},{testItem.TargetPrice}");
            simService.SimulateItem(testItem, preset.Config.Core.WipePrices.Enabled ? testDate : null);
            testDate = testDate.AddMinutes(preset.Config.Core.UpdateInterp);
        }

        string debugPath = Path.Join(preset.ModPath, "debug");
        
        if (!Directory.Exists(debugPath))
            Directory.CreateDirectory(debugPath);
        
        await File.WriteAllTextAsync(Path.Join(debugPath, $"{preset.Config.Core.DebugItem}.csv"), csv.ToString());
        
        logger.Success($"[FleaSimulator] Debug simulation at state {itemDataService.CurrentState.WipeState} completed " +
                       $"with {iterationCount} iterations. CSV saved to mod/debug directory");
    }
}