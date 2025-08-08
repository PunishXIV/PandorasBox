using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Bindings.ImGui;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using PandorasBox.UI;
using System;
using System.Numerics;

namespace PandorasBox.Features.UI
{
    public unsafe class ReduceAll : Feature
    {
        public override string Name => "Reduce All Items";

        public override string Description => "Adds a button to Aetherial Reduction to process all items.";

        public override FeatureType FeatureType => FeatureType.UI;

        internal Overlays Overlay;

        internal bool Reducing;
        public override void Enable()
        {
            Overlay = new(this);
            base.Enable();
        }

        public override bool DrawConditions()
        {
            return Svc.GameGui.GetAddonByName("PurifyItemSelector", 1) != IntPtr.Zero;
        }

        public override void Draw()
        {
            if (Svc.GameGui.GetAddonByName("PurifyItemSelector", 1) != IntPtr.Zero)
            {
                var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("PurifyItemSelector", 1).Address;
                if (addon == null)
                    return;

                if (!addon->IsVisible)
                {
                    Reducing = false;
                    TaskManager.Abort();
                    TaskManager.Enqueue(() => YesAlready.Unlock());
                    return;
                }

                var node = addon->UldManager.NodeList[5];

                if (node == null)
                    return;

                if (node->IsVisible())
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

                if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Mounted])
                {
                    ImGui.Text("You are mounted, please dismount");
                }
                else
                {
                    if (!Reducing)
                    {
                        if (ImGui.Button($"Reduce All###StartReduce", size))
                        {
                            Reducing = true;
                            TaskManager.Enqueue(() => YesAlready.Lock());
                            TaskManager.Enqueue(() => TryReduceAll());
                            TaskManager.Enqueue(() => YesAlready.Unlock());
                        }
                    }
                    else
                    {
                        if (ImGui.Button($"Reducing. Click to abort.###AbortReduce", size))
                        {
                            Reducing = false;
                            TaskManager.Abort();
                            TaskManager.Enqueue(() => YesAlready.Unlock());
                        }
                    }
                }
                ImGui.End();
                ImGui.PopStyleVar(5);
                ImGui.GetFont().Scale = oldSize;
                ImGui.PopFont();
                ImGui.PopStyleColor();

            }
            else
            {
                Reducing = false;
                TaskManager.Abort();
                TaskManager.Enqueue(() => YesAlready.Unlock());
            }
        }

        private void TryReduceAll()
        {
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("PurifyItemSelector", 1).Address;
            if (addon != null)
            {
                var length = addon->UldManager.NodeList[3]->GetAsAtkComponentList()->ListLength;

                for (var i = 1; i <= length; i++)
                {
                    TaskManager.InsertMulti([new(() => SelectFirstItem(addon)), new(ConfirmDialog)]);
                }
                TaskManager.Insert(() => { Reducing = false; return true; });
            }
        }

        private bool? ConfirmDialog()
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied39]) return false;
            if (Svc.GameGui.GetAddonByName("PurifyResult",1) != IntPtr.Zero)
            {
                var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("PurifyResult",1).Address;
                addon->Close(true);
                return true;
            }

            return false;
        }

        private bool? SelectFirstItem(AtkUnitBase* addon)
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied39]) return false;
            TaskManager.InsertMulti([new(() => EzThrottler.Throttle("Generating", 1000)), new(() => EzThrottler.Check("Generating"))]);

            var values = stackalloc AtkValue[2];
            values[0] = new()
            {
                Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                Int = 12,
            };
            values[1] = new()
            {
                Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                UInt = 0,
            };

            addon->FireCallback(2, values);

            return true;
        }

        public override void Disable()
        {
            P.Ws.RemoveWindow(Overlay);
            Overlay = null!;
            if (Svc.GameGui.GetAddonByName("PurifyItemSelector", 1) != IntPtr.Zero)
            {
                var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("PurifyItemSelector", 1).Address;
                var node = addon->UldManager.NodeList[5];

                node->ToggleVisibility(true);
            }

            base.Disable();
        }
    }
}
