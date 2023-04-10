using ClickLib.Clicks;
using Dalamud.Interface;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
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

        private bool Repairing;

        public override void Enable()
        {
            window = new(this);
            P.Ws.AddWindow(window);
            base.Enable();
        }

        public override void Draw()
        {
            if (Svc.GameGui.GetAddonByName("Repair", 1) != IntPtr.Zero)
            {
                var ptr = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Repair", 1);

                var node = ptr->UldManager.NodeList[24];

                if (node == null)
                    return;

                if (node->IsVisible)
                    node->ToggleVisibility(false);

                var position = AtkResNodeHelper.GetNodePosition(node);
                var scale = AtkResNodeHelper.GetNodeScale(node);
                var size = new Vector2(node->Width, node->Height) * scale;

                ImGuiHelpers.ForceNextWindowMainViewport();
                ImGuiHelpers.SetNextWindowPosRelativeMainViewport(position);

                ImGui.PushStyleColor(ImGuiCol.WindowBg, 0);
                float oldSize = ImGui.GetFont().Scale;
                ImGui.GetFont().Scale *= scale.X;
                ImGui.PushFont(ImGui.GetFont());
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0f.Scale());
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0f.Scale(), 0f.Scale()));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0f.Scale(), 0f.Scale()));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f.Scale());
                ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, size);
                ImGui.Begin($"###RepairAll{node->NodeID}", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoNavFocus
                    | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings);


                if (!Repairing)
                {
                    if (ImGui.Button($"Repair All###StartRepair", size))
                    {
                        Repairing = true;
                        TryRepairAll();
                    }

                    ptr->UldManager.NodeList[22]->GetAsAtkTextNode()->SetText(Svc.Data.GetExcelSheet<Addon>().First(x => x.RowId == 856).Text);
                }
                else
                {
                    if (ImGui.Button($"Repairing. Click to abort.###AbortRepair", size))
                    {
                        Repairing = false;
                        P.TaskManager.Abort();
                    }
                    ptr->UldManager.NodeList[22]->GetAsAtkTextNode()->SetText($"{Svc.Data.GetExcelSheet<Addon>().First(x => x.RowId == 856).Text} - Processing {P.TaskManager.NumQueuedTasks} tasks");
                }

                ImGui.End();
                ImGui.PopStyleVar(5);
                ImGui.GetFont().Scale = oldSize;
                ImGui.PopFont();
                ImGui.PopStyleColor();
            };
        }

        private void TryRepairAll()
        {
            for (int i = 1; i <= 7; i++)
            {
                var val = i;
                P.TaskManager.Enqueue(() => SwitchSection(val));
                P.TaskManager.Enqueue(() => Repair(), 300, false);
                P.TaskManager.Enqueue(() => ConfirmYesNo(), 300, false);
            }
            P.TaskManager.Enqueue(() => { Repairing = false; return true; });
        }

        private bool SwitchSection(int section)
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied39]) return false;
            if (TryGetAddonByName<AddonRepair>("Repair", out var addon) && addon->AtkUnitBase.IsVisible)
            {
                var values = stackalloc AtkValue[2];
                values[0] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 1
                };
                values[1] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = section - 1
                };
                addon->AtkUnitBase.FireCallback(2, values);

                return true;

            }
            else
            {
                return false;
            }
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
                new ClickRepair((IntPtr)addon).RepairAll();

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
                addon->YesButton->IsEnabled &&
                addon->AtkUnitBase.UldManager.NodeList[15]->IsVisible)
            {
                new ClickSelectYesNo((IntPtr)addon).Yes();
                return true;
            }

            return false;
        }

        public override void Disable()
        {
            window.IsOpen = false;
            P.Ws.RemoveWindow(window);
            if (Svc.GameGui.GetAddonByName("Repair", 1) != IntPtr.Zero)
            {
                var ptr = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Repair", 1);

                var node = ptr->UldManager.NodeList[24];

                if (node == null)
                    return;

                node->ToggleVisibility(true);
            }

            base.Disable();
        }
    }
}
