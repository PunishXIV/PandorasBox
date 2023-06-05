using Dalamud.Game.ClientState.Conditions;
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

        private bool isNormalNode;
        private bool isFirstCall = true;

        // bool to track we're in the same gathering window
        // bool if this is a non normal mode by presence of checkbox
        // get the AtkComponentCheckbox is checked for each number (1-8 is the actual order)
        // get the gathereditemid[num] of the same node
        // check if it's a collectable via sheets
        private void RunFeature(Dalamud.Game.Framework framework)
        {
            var addon = Svc.GameGui.GetAddonByName("Gathering");
            if (addon == IntPtr.Zero)
            {
                isFirstCall = true;
                return;
            }

            var ptr = (AtkUnitBase*)addon;
            if (ptr == null) return;


            var checkboxNode = ptr->UldManager.NodeList[10]; // quick gather checkbox

            if (isFirstCall)
            {
                if (checkboxNode->IsVisible)
                {
                    isNormalNode = true;
                    isFirstCall = false;
                }
                else
                {
                    isNormalNode = false;
                    isFirstCall = false;
                }
            }
            else
            {
                isNormalNode = (checkboxNode->IsVisible) ? true : false;
            }


            if (!isNormalNode)
            {
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
                    if (isChecked)
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
            // if (flag == ConditionFlag.Gathering && !value) isFirstCall = true;
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
