using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using ECommons;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using PandorasBox.FeaturesSetup;
using System;
using System.Linq;

namespace PandorasBox.Features.UI
{
    internal class DisableKnownGlam : Feature
    {
        public override string Name => "Disable Known Glamours in Glamour Creation";

        public override string Description => "Disables items from being transferred to the glamour dresser if it already exists in it";

        public override FeatureType FeatureType => FeatureType.UI;

        public override void Enable()
        {
            Svc.AddonLifecycle.RegisterListener(AddonEvent.PostUpdate, "MiragePrismPrismBoxCrystallize", DisableGlams);
            Svc.AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "MiragePrismPrismBoxCrystallize", ReenableNodes);
            base.Enable();
        }

        private unsafe void ReenableNodes(AddonEvent type, AddonArgs args)
        {
            var list = ((AtkUnitBase*)args.Addon.Address)->GetNodeById(11)->GetAsAtkComponentTreeList();
            for (int i = 0; i <= 32; i++)
            {
                try
                {
                    var renderer = list->GetItemRenderer(i);
                    renderer->GetAtkResNode()->ToggleVisibility(true);
                    renderer->AtkComponentButton.SetEnabledState(true);
                }
                catch
                {

                }
            }
        }

        private unsafe void DisableGlams(AddonEvent type, AddonArgs args)
        {
            try
            {
                var ins = AgentMiragePrismPrismBox.Instance();
                var list = ((AtkUnitBase*)args.Addon.Address)->GetNodeById(11)->GetAsAtkComponentTreeList();
                var prismList = ins->Data->PrismBoxItems.ToArray();
                var catalystList = ins->Data->CrystallizeItems.ToArray().Where(x => x.ItemId > 0).Select(x => x.ItemId > 1_000_000 ? x.ItemId - 1_000_000 : x.ItemId).Take(ins->Data->CrystallizeItemCount);
                var categories = catalystList.Select(x => Svc.Data.GetExcelSheet<Item>().GetRow(x).EquipSlotCategory.RowId).Distinct().Where(x => x > 0);

                foreach (var (it, i) in catalystList.WithIndex())
                {
                    if (it == 0)
                        continue;

                    var item = Svc.Data.GetExcelSheet<Item>().GetRow(it);
                    var sources = sheetManager?.ItemInfoCache.GetItemUses(it);
                    var isOutfit = sources?.Any(x => x.Type is AllaganLib.GameSheets.Caches.ItemInfoType.GlamourReadySetItem) == true;
                    for (int p = 0; p <= 32; p++)
                    {
                        try
                        {
                            var renderer = list->GetItemRenderer(p);
                            if (renderer is null)
                                continue;

                            var itemName = item.Name;
                            var nodeText = renderer->ButtonTextNode->NodeText.GetText();

                            if (nodeText == itemName)
                            {
                                var hasInDresser = prismList.Any(x => x.ItemId == it);
                                if (hasInDresser)
                                {
                                    var btnNode = renderer->GetNodeById(4);
                                    renderer->GetAtkResNode()->ToggleVisibility(false);
                                    renderer->AtkComponentButton.SetEnabledState(false);
                                    if (btnNode != null && isOutfit)
                                        btnNode->ToggleVisibility(false);

                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //ex.Log();
            }
        }

        public override void Disable()
        {
            Svc.AddonLifecycle.UnregisterListener(DisableGlams);
            Svc.AddonLifecycle.UnregisterListener(ReenableNodes);
            base.Disable();
        }
    }
}
