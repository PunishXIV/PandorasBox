using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PandorasBox.Features.UI
{
    public unsafe class ReEnableQuickGather : Feature
    {
        public override string Name => "ReEnable Quick Gather";

        public override string Description => "This adds back the quick gather button on timed nodes and leve nodes.";

        public override FeatureType FeatureType => FeatureType.UI;

        public override void Enable()
        {
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        private void RunFeature(Dalamud.Game.Framework framework)
        {
            if (Svc.GameGui.GetAddonByName("Gathering") != IntPtr.Zero)
            {
                var ptr = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Gathering");
                if (ptr == null) return;

                var nearbyNodes = Svc.Objects.Where(x => (x.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.GatheringPoint || x.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.CardStand) && GameObjectHelper.GetTargetDistance(x) < 2 && GameObjectHelper.GetHeightDifference(x) <= 3).ToList();

                var nearestNode = nearbyNodes.OrderBy(GameObjectHelper.GetTargetDistance).First();

                // only function on Unspoiled nodes
                if (!(Svc.Data.GetExcelSheet<GatheringPointTransient>().Any(x => x.RowId == nearestNode.DataId && x.GatheringRarePopTimeTable.Value.RowId > 0))) return;

                var checkboxNode = ptr->UldManager.NodeList[10]; // quick gather checkbox
                checkboxNode->ToggleVisibility(true);
                try
                {
                    bool isChecked = checkboxNode->GetAsAtkComponentCheckBox()->IsChecked;
                    if (!isChecked) return;

                    var items = new List<IntPtr>
                    {
                      (IntPtr)ptr->UldManager.NodeList[25]->GetAsAtkComponentNode()->Component->UldManager.NodeList[1],
                      (IntPtr)ptr->UldManager.NodeList[24]->GetAsAtkComponentNode()->Component->UldManager.NodeList[1],
                      (IntPtr)ptr->UldManager.NodeList[23]->GetAsAtkComponentNode()->Component->UldManager.NodeList[1],
                      (IntPtr)ptr->UldManager.NodeList[22]->GetAsAtkComponentNode()->Component->UldManager.NodeList[1],
                      (IntPtr)ptr->UldManager.NodeList[21]->GetAsAtkComponentNode()->Component->UldManager.NodeList[1],
                      (IntPtr)ptr->UldManager.NodeList[20]->GetAsAtkComponentNode()->Component->UldManager.NodeList[1],
                      (IntPtr)ptr->UldManager.NodeList[19]->GetAsAtkComponentNode()->Component->UldManager.NodeList[1],
                      (IntPtr)ptr->UldManager.NodeList[18]->GetAsAtkComponentNode()->Component->UldManager.NodeList[1]
                    };

                    int gatheredNode = -1;
                    for (var i = 0; i < items.Count; i++)
                    {
                        AtkResNode* item = (AtkResNode*)items[i];
                        if (item->IsVisible)
                        {
                            gatheredNode = i;
                        }
                    }
                    if (gatheredNode == -1) return;
                    while (isChecked)
                    {
                        TaskManager.DelayNext("GatheringDelay", 100);
                        TaskManager.Enqueue(() => Callback.Fire(ptr, false, gatheredNode));
                    }
                }
                catch
                {
                    return;
                }
            }
        }

        public override void Disable()
        {
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }
    }
}
