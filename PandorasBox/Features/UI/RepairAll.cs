using ECommons.Automation.UIInput;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using PandorasBox.UI;
using System;
using System.Linq;
using System.Numerics;
using static ECommons.GenericHelpers;

namespace PandorasBox.Features
{
    public unsafe class RepairAll : Feature
    {
        public override string Name => "Replace Repair All";

        public override string Description => "Replace the Repair All button with one that will repair all your equipment, regardless of which dropdown is selected.";

        public override FeatureType FeatureType => FeatureType.UI;

        internal Overlays window;

        private bool Repairing { get; set; }

        public override void Enable()
        {
            window = new(this);
            base.Enable();
        }

        public override bool DrawConditions()
        {
            return TryGetAddonByName<AddonRepair>("Repair", out var _);
        }

        public override void Draw()
        {
            try
            {
                if (TryGetAddonByName<AddonRepair>("Repair", out var addon))
                {
                    if (!addon->IsVisible)
                    {
                        Repairing = false;
                        TaskManager.Abort();
                        TaskManager.Enqueue(() => YesAlready.Unlock());
                        return;
                    }

                    var node = addon->RepairAllButton->AtkComponentBase.AtkResNode->ParentNode;

                    if (node == null)
                        return;

                    node->ToggleVisibility(false);

                    var position = AtkResNodeHelper.GetNodePosition(node);
                    var scale = AtkResNodeHelper.GetNodeScale(node);
                    var size = new Vector2(node->Width, node->Height) * scale;

                    ImGuiHelpers.ForceNextWindowMainViewport();
                    ImGuiHelpers.SetNextWindowPosRelativeMainViewport(position);

                    ImGui.PushStyleColor(ImGuiCol.WindowBg, 0);
                    var oldSize = ImGui.GetFont().Scale;
                    ImGui.GetFont().Scale *= scale.X;
                    ImGui.PushFont(ImGui.GetFont());
                    ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0f.Scale());
                    ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0f.Scale(), 0f.Scale()));
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0f.Scale(), 0f.Scale()));
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f.Scale());
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, size);
                    ImGui.Begin($"###RepairAll{node->NodeId}", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoNavFocus
                        | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings);


                    if (!Repairing)
                    {
                        if (ImGui.Button($"Repair All###StartRepair", size))
                        {
                            Repairing = true;
                            TaskManager.Enqueue(() => YesAlready.Lock());
                            TaskManager.Enqueue(() => TryRepairAll());
                            TaskManager.Enqueue(() => YesAlready.Unlock());
                        }

                        addon->AtkUnitBase.UldManager.NodeList[22]->GetAsAtkTextNode()->SetText(Svc.Data.GetExcelSheet<Addon>().First(x => x.RowId == 856).Text);
                    }
                    else
                    {
                        if (ImGui.Button($"Repairing. Click to abort.###AbortRepair", size))
                        {
                            Repairing = false;
                            TaskManager.Abort();
                            TaskManager.Enqueue(() => YesAlready.Unlock());
                        }
                        addon->AtkUnitBase.UldManager.NodeList[22]->GetAsAtkTextNode()->SetText($"{Svc.Data.GetExcelSheet<Addon>().First(x => x.RowId == 856).Text} - Processing {TaskManager.NumQueuedTasks} tasks");
                    }

                    ImGui.End();
                    ImGui.PopStyleVar(5);
                    ImGui.GetFont().Scale = oldSize;
                    ImGui.PopFont();
                    ImGui.PopStyleColor();
                }
                else
                {
                    Repairing = false;
                    TaskManager.Abort();
                    TaskManager.Enqueue(() => YesAlready.Unlock());
                }
            }
            catch
            {

            }
        }

        private void TryRepairAll()
        {
            for (var i = 1; i <= 7; i++)
            {
                var val = i;
                TaskManager.BeginStack();
                TaskManager.EnqueueWithTimeout(() => Repair(), 300, false);
                TaskManager.EnqueueWithTimeout(() => ConfirmYesNo(), 300, false);
                TaskManager.Enqueue(SwitchSection);
                TaskManager.InsertStack();
            }
            TaskManager.Insert(() => { Repairing = false; return true; });
        }

        private bool SwitchSection()
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied39]) return false;
            if (TryGetAddonByName<AddonRepair>("Repair", out var addon) && addon->AtkUnitBase.IsVisible)
            {
                var fwdBtn = addon->AtkUnitBase.GetNodeById(14)->GetAsAtkComponentButton();
                if (fwdBtn == null) return false;
                if (fwdBtn->IsEnabled)
                {
                    fwdBtn->ClickAddonButton((AtkComponentBase*)addon, 2);

                    return true;
                }

            }

            return false;
        }

        private AtkValue* GenerateDropdownCallback(int one, int two)
        {
            var values = stackalloc AtkValue[2];
            values[0] = new AtkValue()
            {
                Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                Int = one
            };
            values[1] = new AtkValue()
            {
                Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                Int = two
            };

            return values;
        }

        internal static bool Repair()
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied39]) return false;
            if (TryGetAddonByName<AddonRepair>("Repair", out var addon) && addon->AtkUnitBase.IsVisible && addon->RepairAllButton->IsEnabled)
            {
                Svc.Log.Debug($"{addon->RepairAllButton->AtkComponentBase.OwnerNode is null}");
                new AddonMaster.Repair((IntPtr)addon).RepairAll();

                return true;
            }
            return false;
        }

        internal static bool ConfirmYesNo()
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied39]) return false;

            if (TryGetAddonByName<AddonRepair>("Repair", out var r) &&
                r->AtkUnitBase.IsVisible && TryGetAddonByName<AddonSelectYesno>("SelectYesno", out var addon) &&
                addon->AtkUnitBase.IsVisible &&
                addon->AtkUnitBase.UldManager.NodeList[15]->IsVisible())
            {
                try
                {
                    new AddonMaster.SelectYesno((IntPtr)addon).Yes();
                    return true;
                }
                catch (Exception ex)
                {
                    ex.LogWarning();
                    return false;
                }
            }

            return false;
        }

        public override void Disable()
        {
            window.IsOpen = false;
            P.Ws.RemoveWindow(window);
            if (Svc.GameGui.GetAddonByName("Repair", 1) != IntPtr.Zero)
            {
                var ptr = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Repair", 1).Address;

                var node = ptr->UldManager.NodeList[24];

                if (node == null)
                    return;

                node->ToggleVisibility(true);
            }

            base.Disable();
        }
    }


}
