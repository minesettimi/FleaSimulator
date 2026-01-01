using System.Reflection;
using FleaSimulator.Helpers;
using FleaSimulator.Models.Config;
using FleaSimulator.Models.State;
using FleaSimulator.Services;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Generators;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;

namespace FleaSimulator.Generators;

[Injectable(InjectionType.Singleton)]
public class FleaOfferGenerator(RagfairOfferGenerator offerGenerator,
    ItemHelper itemHelper,
    RagfairServerHelper serverHelper,
    PresetHelper presetHelper,
    ConfigServer configServer,
    ItemDataService itemService,
    PresetService presetService,
    MathUtil mathUtil,
    RandomUtil randomUtil,
    ChaosHelper chaosHelper,
    ICloner cloner,
    ISptLogger<FleaOfferGenerator> logger)
{
    private readonly RagfairConfig _ragfairConfig = configServer.GetConfig<RagfairConfig>();
    private readonly MethodBase _removeBannedPlates = typeof(RagfairOfferGenerator)
        .GetMethod("RemoveBannedPlatesFromPreset",  BindingFlags.Instance | BindingFlags.NonPublic)!;
    private readonly MethodBase _createSingleOffer = typeof(RagfairOfferGenerator)
        .GetMethod("CreateSingleOfferForItem",  BindingFlags.Instance | BindingFlags.NonPublic)!;
    
    public void CreateOffersFromAssorts(List<Item> assortItems, bool isExpired)
    {
        Item? rootItem = assortItems.FirstOrDefault();

        if (rootItem == null)
            return;

        KeyValuePair<bool, TemplateItem?> sellDetails = itemHelper.GetItem(rootItem.Template);

        if (!serverHelper.IsItemValidRagfairItem(sellDetails))
            return;
        
        bool isPreset = rootItem.Upd?.SptPresetId is not null && presetHelper.IsPreset(rootItem.Upd.SptPresetId.Value);
        if (isPreset && _ragfairConfig.Dynamic.Blacklist.EnableBsgList)
        {
            _removeBannedPlates.Invoke(offerGenerator, [assortItems, _ragfairConfig.Dynamic.Blacklist.ArmorPlate]);
        }
        
        //override offer counts
        ItemState itemState = itemService.CurrentState.Items[sellDetails.Value!.Id];
        CategoryConfig category = itemState.Category;
        
        BuyConfig buyConfig = presetService.Config.Core.BuyConfig;

        int offerTotal = 1;

        if (!isExpired)
        {
            offerTotal = (int)Math.Round(mathUtil.MapToRange(category.Supply, 0,
                1.0, buyConfig.SupplyMinOffers, buyConfig.SupplyMaxOffers));

        
            int offerDiff = randomUtil.GetInt(-buyConfig.SupplyOfferOffset, buyConfig.SupplyOfferOffset);
            offerDiff = chaosHelper.ChaosShift(category, offerDiff);
        
            offerTotal += offerDiff;
            offerTotal = Math.Max(offerTotal, buyConfig.CanHaveNoOffers ? 0 : 1);
        }
        
        for (int i = 0; i < offerTotal; i++)
        {
            List<Item> cloneAssort = cloner.Clone(assortItems)!;
            itemHelper.ReparentItemAndChildren(cloneAssort[0], cloneAssort);

            cloneAssort[0].ParentId = null;
            cloneAssort[0].SlotId = null;

            _createSingleOffer.Invoke(offerGenerator, [
                new MongoId(),
                cloneAssort,
                isPreset,
                sellDetails.Value,
                false,
                OfferCreator.FakePlayer
            ]);
        }
    }
}