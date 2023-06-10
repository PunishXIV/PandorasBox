using Dalamud.Game.ClientState.Conditions;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static ECommons.GenericHelpers;

namespace PandorasBox.Features.UI
{
    public unsafe class ReEnableQuickGather : Feature
    {
        public override string Name => "ReEnable Quick Gather";

        public override string Description => "This adds back the quick gather button on timed nodes and leve nodes.";

        public override FeatureType FeatureType => FeatureType.UI;

        public enum GatheringPointType : byte
        {
            None,
            Basic,
            Unspoiled,
            Leve,
            Ephemeral,  // for aetherial reduction,
            Folklore,
            SFShadow,   // spearfishing special
            DiademBasic,
            DiademClouded,  // diadem special
        }

        private int gatheredItemIndex = -1;

        private void RunFeature(Dalamud.Game.Framework framework)
        {
            try
            {
                if (TryGetAddonByName<AddonGathering>("Gathering", out var addon))
                {
                    var ptr = (AtkUnitBase*)addon;
                    if (ptr == null) return;

                    // return if it's not an unspoiled, leve, ephemeral, or folklore node
                    var targetNode = Svc.Targets.Target;
                    if (!(Svc.Data.GetExcelSheet<GatheringPoint>().Any(x => x.RowId == targetNode.DataId && x.Type is 2 or 3 or 4 or 5))) return;

                    var checkboxNode = ptr->UldManager.NodeList[10];
                    checkboxNode->ToggleVisibility(true);

                    bool isChecked = checkboxNode->GetAsAtkComponentCheckBox()->IsChecked;
                    if (!isChecked) { TaskManager.Abort(); gatheredItemIndex = -1; return; }

                    var gatherablesSlots = new List<bool>
                    {
                      ptr->UldManager.NodeList[25]->GetAsAtkComponentCheckBox()->IsChecked,
                      ptr->UldManager.NodeList[24]->GetAsAtkComponentCheckBox()->IsChecked,
                      ptr->UldManager.NodeList[23]->GetAsAtkComponentCheckBox()->IsChecked,
                      ptr->UldManager.NodeList[22]->GetAsAtkComponentCheckBox()->IsChecked,
                      ptr->UldManager.NodeList[21]->GetAsAtkComponentCheckBox()->IsChecked,
                      ptr->UldManager.NodeList[20]->GetAsAtkComponentCheckBox()->IsChecked,
                      ptr->UldManager.NodeList[19]->GetAsAtkComponentCheckBox()->IsChecked,
                      ptr->UldManager.NodeList[18]->GetAsAtkComponentCheckBox()->IsChecked
                    };

                    var gatherablesIds = new List<uint> {
                        addon->GatheredItemId1, addon->GatheredItemId2, addon->GatheredItemId3, addon->GatheredItemId4, addon->GatheredItemId5, addon->GatheredItemId6, addon->GatheredItemId7, addon->GatheredItemId8
                    };
                    for (var i = 0; i < gatherablesSlots.Count; i++)
                    {
                        if (gatherablesSlots[i])
                        {
                            gatheredItemIndex = i;
                        }
                    }

                    if (gatheredItemIndex == -1) return;

                    var gatheredItem = Svc.Data.GetExcelSheet<Item>().Where(x => x.RowId == gatherablesIds[gatheredItemIndex]).First();
                    if (gatheredItem.IsCollectable)
                    {
                        checkboxNode->GetAsAtkComponentNode()->Component->UldManager.NodeList[2]->ToggleVisibility(false);
                        gatheredItemIndex = -1;
                        return;
                    }

                    // do I even need to do this here or can I just keep using &addon->AtkUnitBase in the callback?
                    var gatheringWindow = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Gathering", 1);
                    if (gatheringWindow == null) return;
                    if (isChecked)
                    {
                        // why does calling isChecked in here not work?
                        // this will *sometimes* generate a NRE if it fires off in the wrong tick after the last gather. Doesn't cause issues aside from polluting the log though
                        TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Gathering42] && checkboxNode->GetAsAtkComponentCheckBox()->IsChecked);
                        TaskManager.DelayNext("InteractCooldown", 100);
                        TaskManager.Enqueue(() => Callback.Fire(gatheringWindow, false, gatheredItemIndex));
                    }
                }
                else
                {
                    gatheredItemIndex = -1;
                    TaskManager.Abort();
                }
            }
            catch
            {
                return;
            }
        }

        public override void Enable()
        {
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        public override void Disable()
        {
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }
    }
}
