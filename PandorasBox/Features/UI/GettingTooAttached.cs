using PandorasBox.Features;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using PandorasBox.UI;
using ClickLib.Clicks;
using Dalamud.Interface;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using System;
using System.Numerics;
using Dalamud.Interface.Components;
using ECommons.Logging;
using Dalamud.Interface.Windowing;
using ECommons;

namespace PandorasBox.Features.UI
{
    public unsafe class GettingTooAttached : Feature
    {
        public override string Name => "Getting Too Attached";

        public override string Description => "Adds a button to the materia melding window to loop melding for the Getting Too Attached achievement.";

        public override FeatureType FeatureType => FeatureType.UI;

        internal Overlays OverlayWindow;

        internal bool GTALooping = false;
        private int achievementProgress = 99999;

        public override void Enable()
        {
            OverlayWindow = new(this);
            P.Ws.AddWindow(OverlayWindow);
            base.Enable();
        }

        public override void Draw()
        {
            if (Svc.GameGui.GetAddonByName("MateriaAttach", 1) != IntPtr.Zero)
            {
                var ptr = (AtkUnitBase*)Svc.GameGui.GetAddonByName("MateriaAttach", 1);

                var node = ptr->UldManager.NodeList[0];

                if (node == null)
                    return;

                // if (node->IsVisible)
                //     node->ToggleVisibility(false);

                var position = AtkResNodeHelper.GetNodePosition(node);
                var scale = AtkResNodeHelper.GetNodeScale(node) / 25;
                var size = new Vector2(node->Width * 4, node->Height) * scale;
                Vector2 vector2 = new Vector2(-(float)node->Width / 10, (float)node->Height) * scale;

                ImGuiHelpers.ForceNextWindowMainViewport();
                ImGuiHelpers.SetNextWindowPosRelativeMainViewport(position - vector2);

                ImGui.PushStyleColor(ImGuiCol.WindowBg, 0);
                float oldSize = ImGui.GetFont().Scale;
                ImGui.GetFont().Scale *= scale.X * 25;
                ImGui.PushFont(ImGui.GetFont());
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 5f.Scale());
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0f.Scale(), 0f.Scale()));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0f.Scale(), 0f.Scale()));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f.Scale());
                ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, 0);
                ImGui.Begin($"###LoopMelding{node->NodeID}", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoNavFocus
                    | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings);


                if (!GTALooping)
                {
                    if (ImGui.Button($"GettingTooAttached###StartLooping", size))
                    {
                        GTALooping = true;
                        this.TaskManager.Enqueue((() => YesAlready.DisableIfNeeded()));
                        this.TaskManager.Enqueue((() => this.TryGettingTooAttached()));
                    }
                }
                else
                {
                    if (ImGui.Button($"Looping. Click to abort.###AbortLoop", size))
                    {
                        GTALooping = false;
                        TaskManager.Abort();
                        this.TaskManager.Enqueue((() => YesAlready.EnableIfNeeded()));
                    }
                }

                ImGui.End();
                ImGui.PopStyleVar(4);
                ImGui.GetFont().Scale = oldSize;
                ImGui.PopFont();
                ImGui.PopStyleColor();
            }
        }


        private void TryGettingTooAttached()
        {
            if (this.achievementProgress > 0)
            {
                TaskManager.Enqueue(() => SelectItem(), "Selecting Item");
                TaskManager.Enqueue(() => SelectMateria(), "Selecting Materia");
                TaskManager.Enqueue(() => ConfirmMateriaDialog(), "Confirming Materia Dialog");
                TaskManager.Enqueue(() => RetrieveMateria(), "Navigating Retrieval Menu");
                TaskManager.Enqueue(() => ConfirmRetrievalDialog(), "Retrieving Materia");
                TaskManager.DelayNext("WaitForDelay", 400);
                this.achievementProgress -= 1;
                TaskManager.Enqueue(() => TryGettingTooAttached(), "Repeat Loop");
            }
            else
            {
                TaskManager.Enqueue(() => GTALooping = false);
                this.TaskManager.Enqueue((() => YesAlready.EnableIfNeeded()));
            }
        }

        private unsafe bool? SelectItem()
        {
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("MateriaAttach", 1);
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied39] || !Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.NormalConditions] || addon == null || !GenericHelpers.IsAddonReady(addon)) return false;
            TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Selecting Item", 300));
            TaskManager.EnqueueImmediate(() => EzThrottler.Check("Selecting Item"));
            try
            {
                var materializePTR = Svc.GameGui.GetAddonByName("MateriaAttach", 1);
                if (materializePTR == IntPtr.Zero)
                    return false;

                var materalizeWindow = (AtkUnitBase*)materializePTR;
                if (materalizeWindow == null)
                    return false;


                var SelectItemvalues = stackalloc AtkValue[4];
                SelectItemvalues[0] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 1,
                };
                SelectItemvalues[1] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 0,
                };
                SelectItemvalues[2] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 1,
                };

                SelectItemvalues[3] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 0,
                };

                materalizeWindow->FireCallback(1, SelectItemvalues);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsMateriaMenuDialogOpen() => Svc.GameGui.GetAddonByName("MateriaAttachDialog", 1) != IntPtr.Zero;

        public unsafe bool? SelectMateria()
        {
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("MateriaAttach", 1);
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied39] || !Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.NormalConditions] || IsMateriaMenuDialogOpen() || !GenericHelpers.IsAddonReady(addon)) return false;
            TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Selecting Materia", 300));
            TaskManager.EnqueueImmediate(() => EzThrottler.Check("Selecting Materia"));
            try
            {
                var materializePTR = Svc.GameGui.GetAddonByName("MateriaAttach", 1);
                if (materializePTR == IntPtr.Zero)
                    return false;

                var materalizeWindow = (AtkUnitBase*)materializePTR;
                if (materalizeWindow == null)
                    return false;

                var SelectMateriavalues = stackalloc AtkValue[4];
                SelectMateriavalues[0] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 2,
                };
                SelectMateriavalues[1] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 0,
                };
                SelectMateriavalues[2] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 1,
                };

                SelectMateriavalues[3] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 0,
                };

                materalizeWindow->FireCallback(1, SelectMateriavalues);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public unsafe bool ConfirmMateriaDialog()
        {
            if (!IsMateriaMenuDialogOpen()) return false;
            TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Confirming Materia Dialog", 1000));
            TaskManager.EnqueueImmediate(() => EzThrottler.Check("Confirming Materia Dialog"));
            try
            {
                var addon = Svc.GameGui.GetAddonByName("MateriaAttachDialog", 1);
                if (addon == IntPtr.Zero)
                    return false;

                var meldDialogWindow = (AtkUnitBase*)addon;
                if (meldDialogWindow == null)
                    return false;

                // Confirming the meld
                var MeldDialogvalues = stackalloc AtkValue[4];
                MeldDialogvalues[0] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 0,
                };
                MeldDialogvalues[1] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 0,
                };
                MeldDialogvalues[2] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 0,
                };

                meldDialogWindow->FireCallback(1, MeldDialogvalues);
                meldDialogWindow->Close(true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsMateriaRetrieveDialogOpen() => Svc.GameGui.GetAddonByName("MateriaRetrieveDialog", 1) != IntPtr.Zero;

        public unsafe bool RetrieveMateria()
        {
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("MateriaAttach", 1);
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.MeldingMateria] || !Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.NormalConditions] || !GenericHelpers.IsAddonReady(addon)) return false;
            try
            {
                var materializePTR = Svc.GameGui.GetAddonByName("MateriaAttach", 1);
                if (materializePTR == IntPtr.Zero)
                    return false;

                var materalizeWindow = (AtkUnitBase*)materializePTR;
                if (materalizeWindow == null)
                    return false;


                var SelectItemvalues = stackalloc AtkValue[4];
                SelectItemvalues[0] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 1,
                };
                SelectItemvalues[1] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 0,
                };
                SelectItemvalues[2] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 1,
                };

                SelectItemvalues[3] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 0,
                };

                materalizeWindow->FireCallback(1, SelectItemvalues);
                TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Navigating Retrival", 300));
                TaskManager.EnqueueImmediate(() => EzThrottler.Check("Navigating Retrieval"));

                var RightClickCallback = stackalloc AtkValue[4];
                RightClickCallback[0] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 4,
                };
                RightClickCallback[1] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 0,
                };
                RightClickCallback[2] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                    UInt = 1,
                };

                RightClickCallback[3] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 0,
                };

                materalizeWindow->FireCallback(1, RightClickCallback);
                TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Navigating Retrival", 300));
                TaskManager.EnqueueImmediate(() => EzThrottler.Check("Navigating Retrieval"));

                var contextPTR = Svc.GameGui.GetAddonByName("ContextMenu", 1);
                if (contextPTR == IntPtr.Zero)
                    return false;

                var contextWindow = (AtkUnitBase*)contextPTR;
                if (contextWindow == null)
                    return false;

                var RetrieveCallback = stackalloc AtkValue[5];
                RetrieveCallback[0] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 0,
                };
                RetrieveCallback[1] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 1,
                };
                RetrieveCallback[2] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                    UInt = 0,
                };
                RetrieveCallback[3] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 0,
                };
                RetrieveCallback[4] = new()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 0,
                };

                contextWindow->FireCallback(1, RetrieveCallback);
                TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Navigating Retrival", 300));
                TaskManager.EnqueueImmediate(() => EzThrottler.Check("Navigating Retrieval"));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public unsafe bool ConfirmRetrievalDialog()
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied39] || Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.MeldingMateria] || !IsMateriaRetrieveDialogOpen()) return false;
            try
            {
                var retrievePTR = Svc.GameGui.GetAddonByName("MateriaRetrieveDialog", 1);
                if (retrievePTR == IntPtr.Zero)
                    return false;

                var retrievalWindow = (AtkUnitBase*)retrievePTR;
                if (retrievalWindow == null)
                    return false;

                ClickMateriaRetrieveDialog.Using(retrievePTR).Begin();
                TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Retrieving", 2000));
                TaskManager.EnqueueImmediate(() => EzThrottler.Check("Retrieving"));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override void Disable()
        {
            P.Ws.RemoveWindow(OverlayWindow);
            OverlayWindow = null;
            if (Svc.GameGui.GetAddonByName("MateriaAttach", 1) != IntPtr.Zero)
            {
                var ptr = (AtkUnitBase*)Svc.GameGui.GetAddonByName("MateriaAttach", 1);

                var node = ptr->UldManager.NodeList[2];

                if (node == null)
                    return;

                node->ToggleVisibility(true);
            }

            base.Disable();
        }
    }
}
