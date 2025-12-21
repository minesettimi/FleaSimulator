using System.Reflection;
using fleasimulator.Models.Config;
using FleaSimulator.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;

namespace FleaSimulator.Helpers;

[Injectable]
public class SimItemHelper(
    ISptLogger<SimItemHelper> logger,
    PresetService preset)
{
    public CategoryConfig RetrieveItemCategory(TemplateItem item)
    {
        CategoryConfig? resultCategory = null;
        ItemConfig itemConfig = preset.Config.Items;

        //first try to get the item itself, otherwise try to get its parent
        if (itemConfig.Individual.TryGetValue(item.Id, out string? categoryName) 
            || itemConfig.Parents.TryGetValue(item.Parent, out categoryName))
        {
            resultCategory = preset.Config.Categories.GetValueOrDefault(categoryName);
        }
        
        return resultCategory ?? preset.DefaultCategoryConfig;
    }
}