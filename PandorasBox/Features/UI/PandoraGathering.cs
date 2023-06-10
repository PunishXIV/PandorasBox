using ClickLib.Bases;
using ClickLib.Structures;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using PandorasBox.UI;
using PandorasBox.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using static ECommons.GenericHelpers;

namespace PandorasBox.Features.UI
{
    public unsafe class PandoraGathering : Feature
    {

        private delegate void GatherEventDelegate(IntPtr a1, ulong a2, IntPtr a3, ulong a4);
        private HookWrapper<GatherEventDelegate> gatherEventHook;

        private delegate void QuickGatherToggleDelegate(AddonGathering* a1);
        private HookWrapper<QuickGatherToggleDelegate> quickGatherToggle;

        public override string Name => "Pandora Quick Gather";

        public override string Description => "Replaces the Quick Gather checkbox with a new one that enables better quick gathering. Works on all nodes and can be interrupted at any point by disabling the checkbox. Also remembers your settings between sessions.";

        public class Configs : FeatureConfig
        {
            public bool Gathering = false;
        }

        public Configs Config { get; private set; }


        public override FeatureType FeatureType => FeatureType.UI;

        private Overlays Overlay;

        

        private ulong lastGatheredIndex = 10;

        public unsafe override void Draw()
        {
            if (Svc.GameGui.GetAddonByName("Gathering") != IntPtr.Zero)
            {
                var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Gathering");
                if (addon == null || !addon->IsVisible) return;

                var node = addon->UldManager.NodeList[10];

                if (node->IsVisible)
                    node->ToggleVisibility(false);

                var position = AtkResNodeHelper.GetNodePosition(node);
                var scale = AtkResNodeHelper.GetNodeScale(node);
                var size = new Vector2(node->Width, node->Height) * scale;

                ImGuiHelpers.ForceNextWindowMainViewport();
                ImGuiHelpers.SetNextWindowPosRelativeMainViewport(position);

                ImGui.PushStyleColor(ImGuiCol.WindowBg, 0);
                ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGuiColors.DalamudGrey3);
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0f.Scale());
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0f.Scale(), 0f.Scale()));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f.Scale());
                ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, size);
                ImGui.Begin($"###PandoraGathering{node->NodeID}", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoNavFocus
                    | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings);


                if (ImGui.Checkbox("Enable Pandora Gathering", ref Config.Gathering))
                {
                    if (Config.Gathering && node->GetAsAtkComponentCheckBox()->IsChecked)
                        QuickGatherToggle(null);

                    if (!Config.Gathering)
                        TaskManager.Abort();

                    SaveConfig(Config);
                }

                ImGui.End();
                ImGui.PopStyleVar(4);
                ImGui.PopStyleColor(2);

            }
        }

        public override void Enable()
        {
            Overlay = new Overlays(this);
            Config = LoadConfig<Configs>() ?? new Configs();
            gatherEventHook ??= Common.Hook<GatherEventDelegate>("E8 ?? ?? ?? ?? 84 C0 74 ?? EB ?? 48 8B 89", GatherDetour);
            gatherEventHook.Enable();

            quickGatherToggle ??= Common.Hook<QuickGatherToggleDelegate>("e8 ?? ?? ?? ?? eb 3f 4c 8b 4c 24 50", QuickGatherToggle);

            base.Enable();
        }

        private void QuickGatherToggle(AddonGathering* a1)
        {
            if (a1 == null && Svc.GameGui.GetAddonByName("Gathering") != IntPtr.Zero)
                a1 = (AddonGathering*)Svc.GameGui.GetAddonByName("Gathering", 1);
            else
                return;

            a1->QuickGatheringComponentCheckBox->AtkComponentButton.Flags ^= 0x40000;
            quickGatherToggle.Original(a1);
        }

        private void GatherDetour(nint a1, ulong index, nint a3, ulong a4)
        {
            var addon = (AddonGathering*)Svc.GameGui.GetAddonByName("Gathering", 1);
            if (addon != null && Config.Gathering)
            {
                var item = index switch
                {
                    0 => addon->GatheredItemId1,
                    1 => addon->GatheredItemId2,
                    2 => addon->GatheredItemId3,
                    3 => addon->GatheredItemId4,
                    4 => addon->GatheredItemId5,
                    5 => addon->GatheredItemId6,
                    6 => addon->GatheredItemId7,
                    7 => addon->GatheredItemId8
                };

                if (item != lastGatheredIndex)
                {
                    TaskManager.Abort();
                    lastGatheredIndex = item;
                }

                if (!Svc.Data.GetExcelSheet<Item>().GetRow(item).IsCollectable)
                {
                    bool quickGathering = addon->QuickGatheringComponentCheckBox->IsChecked;
                    Dalamud.Logging.PluginLog.Debug($"{quickGathering}");
                    if (quickGathering)
                    {
                        QuickGatherToggle(addon);
                    }

                    var receiveEventAddress = new IntPtr(addon->AtkUnitBase.AtkEventListener.vfunc[2]);
                    var eventDelegate = Marshal.GetDelegateForFunctionPointer<ReceiveEventDelegate>(receiveEventAddress)!;

                    var target = AtkStage.GetSingleton();
                    var eventData = EventData.ForNormalTarget(target, &addon->AtkUnitBase);
                    var inputData = InputData.Empty();

                    TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Gathering42]);
                    TaskManager.Enqueue(() => eventDelegate.Invoke(&addon->AtkUnitBase.AtkEventListener, ClickLib.Enums.EventType.CHANGE, (uint)index, eventData.Data, inputData.Data));
                }

            }
            gatherEventHook.Original(a1, index, a3, a4);

        }

        public override void Disable()
        {
            P.Ws.RemoveWindow(Overlay);
            SaveConfig(Config);
            gatherEventHook?.Disable();
            quickGatherToggle?.Disable();
            base.Disable();
        }
    }
}
