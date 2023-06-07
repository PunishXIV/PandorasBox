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

        private void RunFeature(Dalamud.Game.Framework framework)
        {
            try
            {
                if (TryGetAddonByName<AddonGathering>("Gathering", out var addon))
                {

                    var ptr = (AtkUnitBase*)addon;
                    if (ptr == null) return;

                    // var nearbyNodes = Svc.Objects.Where(x => (x.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.GatheringPoint || x.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.CardStand) && GameObjectHelper.GetTargetDistance(x) < 2 && GameObjectHelper.GetHeightDifference(x) <= 3).ToList();

                    // var nearestNode = nearbyNodes.OrderBy(GameObjectHelper.GetTargetDistance).First();

                    // only function on Unspoiled nodes
                    // if (!(Svc.Data.GetExcelSheet<GatheringPointTransient>().Any(x => x.RowId == nearestNode.DataId && x.GatheringRarePopTimeTable.Value.RowId > 0))) return;

                    // return if it's a normal, diadem, or fishing node
                    var targetNode = Svc.Targets.Target;
                    if (!(Svc.Data.GetExcelSheet<GatheringPoint>().Any(x => x.RowId == targetNode.DataId && x.Type is 2 or 3 or 4 or 5))) return;

                    var checkboxNode = ptr->UldManager.NodeList[10];
                    checkboxNode->ToggleVisibility(true);
                    bool isChecked = checkboxNode->GetAsAtkComponentCheckBox()->IsChecked;
                    if (!isChecked) return;

                    var items = new List<bool>
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

                    // var nodeIntegrity = 4;
                    // var locationEffectNode = ptr->UldManager.NodeList[8];
                    // if (locationEffectNode->IsVisible) nodeIntegrity = 4 + Regex.Match(locationEffectNode->GetAsAtkTextNode()->NodeText.ToString(), @"Gathering Attempts/Integrity \+(\d+)").Groups[1].Value;

                    var gatherablesIds = new List<uint> {
                        addon->GatheredItemId1, addon->GatheredItemId2, addon->GatheredItemId3, addon->GatheredItemId4, addon->GatheredItemId5, addon->GatheredItemId6, addon->GatheredItemId7, addon->GatheredItemId8
                    };
                    int gatheredItemIndex = -1;
                    for (var i = 0; i < items.Count; i++)
                    {
                        if (items[i])
                        {
                            gatheredItemIndex = i;
                        }
                    }

                    // var gatheredItem = Svc.Data.GetExcelSheet<Item>().Where(x => x.RowId == gatherablesIds[gatheredItemIndex]).First();
                    // ideally, if it's a collectable, uncheck the box as a form of in-game feedback that it's invalid
                    if (gatheredItemIndex == -1) return;
                    // unchecking mid operation does nothing?
                    if (isChecked)
                    {
                        TaskManager.DelayNext("GatheringDelay", 100);
                        TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.Gathering] && !Svc.Condition[ConditionFlag.Gathering42]);
                        TaskManager.Enqueue(() => Callback.Fire(ptr, false, gatheredItemIndex));
                    }
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
