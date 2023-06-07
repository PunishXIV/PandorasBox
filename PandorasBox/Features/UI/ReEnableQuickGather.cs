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
            // get rid of the try so I can actually see any errors in execution?
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

                    // this shit idea probably isn't needed now
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

                    if (gatheredItemIndex == -1) return;

                    // ideally, if it's a collectable, uncheck the box as a form of in-game feedback that it's invalid
                    var gatheredItem = Svc.Data.GetExcelSheet<Item>().Where(x => x.RowId == gatherablesIds[gatheredItemIndex]).First();
                    if (gatheredItem.IsCollectable) return;

                    // do I even need to do this here or can I just keep using &addon->AtkUnitBase in the callback?
                    var gatheringWindow = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Gathering", 1);
                    if (gatheringWindow == null) return;
                    if (isChecked)
                    {
                        // why does calling isChecked in here not work?
                        TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Gathering42] && checkboxNode->GetAsAtkComponentCheckBox()->IsChecked);
                        TaskManager.DelayNext("InteractCooldown", 100);
                        TaskManager.Enqueue(() => Callback.Fire(gatheringWindow, false, gatheredItemIndex));
                    }
                    else
                    {
                        // can't reset which item to gather here?
                        gatheredItemIndex = -1;
                        // this abort is probably not needed
                        TaskManager.Abort();
                    }
                }
                else
                {
                    TaskManager.Abort();
                }
            }
            catch
            {
                return;
            }
        }

        // private void TryQuickGather(Dalamud.Game.ClientState.Conditions.ConditionFlag flag, bool value)
        // {
        //     if (flag == Dalamud.Game.ClientState.Conditions.ConditionFlag.Gathering && !value)
        //     {
        //     }
        // }

        // private void TriggerCooldown(ConditionFlag flag, bool value)
        // {
        //     if ((flag == ConditionFlag.Gathering) && !value)
        //     {
        //         TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Gathering42]);
        //         TaskManager.DelayNext("InteractCooldown", 400);
        //         TaskManager.Enqueue(() => Callback.Fire(&addon->AtkUnitBase, false, gatheredItemIndex));
        //     }
        // }

        public override void Enable()
        {
            Svc.Framework.Update += RunFeature;
            // Svc.Condition.ConditionChange += TriggerCooldown;
            base.Enable();
        }

        public override void Disable()
        {
            Svc.Framework.Update -= RunFeature;
            // Svc.Condition.ConditionChange -= TriggerCooldown;
            base.Disable();
        }
    }
}
