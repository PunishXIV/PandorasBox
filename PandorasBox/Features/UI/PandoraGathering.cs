using ClickLib.Bases;
using ClickLib.Structures;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using PandorasBox.UI;
using PandorasBox.Utility;
using System;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using Action = Lumina.Excel.GeneratedSheets.Action;

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

            public bool RememberLastNode = false;

            public bool Use500GPYield = false;

            public bool Use100GPYield = false;
        }

        public Configs Config { get; private set; }


        public override FeatureType FeatureType => FeatureType.UI;

        private Overlays Overlay;



        private ulong lastGatheredIndex = 10;
        private uint lastGatheredItem = 0;

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

                ImGui.GetFont().Scale = scale.X;
                var oldScale = ImGui.GetIO().FontGlobalScale;
                ImGui.GetIO().FontGlobalScale = 0.9f;
                ImGui.PushFont(ImGui.GetFont());


                ImGui.Begin($"###PandoraGathering{node->NodeID}", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoNavFocus
                    | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings);

                ImGui.Columns(2, null, false);

                if (ImGui.Checkbox("Enable Pandora Gathering", ref Config.Gathering))
                {
                    if (Config.Gathering && node->GetAsAtkComponentCheckBox()->IsChecked)
                        QuickGatherToggle(null);

                    if (!Config.Gathering)
                        TaskManager.Abort();

                    SaveConfig(Config);
                }


                ImGui.Checkbox("Remember Item Between Nodes", ref Config.RememberLastNode);

                switch (Svc.ClientState.LocalPlayer.ClassJob.Id)
                {
                    case 17:
                        ImGui.NextColumn();
                        ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>().GetRow(4087).Name.RawString}", ref Config.Use100GPYield);
                        ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>().GetRow(224).Name.RawString}", ref Config.Use500GPYield);
                        break;
                    case 16:
                        ImGui.NextColumn();
                        ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>().GetRow(4073).Name.RawString}", ref Config.Use100GPYield);
                        ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>().GetRow(241).Name.RawString}", ref Config.Use500GPYield);
                        break;
                }


                ImGui.Columns(1);
                ImGui.End();

                ImGui.GetFont().Scale = 1;
                ImGui.GetIO().FontGlobalScale = oldScale;
                ImGui.PopFont();

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

            Common.OnAddonSetup += CheckLastItem;

            base.Enable();
        }

        private void CheckLastItem(SetupAddonArgs obj)
        {
            if (obj.AddonName == "Gathering" && Config.Gathering)
            {
                TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Gathering42]);
                TaskManager.Enqueue(() =>
                {
                    if (Config.Use500GPYield)
                    {
                        TaskManager.Enqueue(() => Use500GPSkill(), "Use500GPSetup");
                        TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Gathering42]);
                    }

                    if (Config.Use100GPYield)
                    {
                        TaskManager.Enqueue(() => Use100GPSkill(), "Use100GPSetup");
                        TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Gathering42]);
                    }

                    if (Config.RememberLastNode)
                    {
                        if (lastGatheredIndex > 7)
                            return;

                        var addon = (AddonGathering*)Svc.GameGui.GetAddonByName("Gathering", 1);
                        var item = lastGatheredIndex switch
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

                        if (item == lastGatheredItem)
                        {
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
                                TaskManager.Enqueue(() => eventDelegate.Invoke(&addon->AtkUnitBase.AtkEventListener, ClickLib.Enums.EventType.CHANGE, (uint)lastGatheredIndex, eventData.Data, inputData.Data));
                            }
                        }
                    }
                });
            }
        }

        private void Use100GPSkill()
        {
            if (Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 1286 || x.StatusId == 756))
                return;

            switch (Svc.ClientState.LocalPlayer.ClassJob.Id)
            {
                case 17:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Spell, 273) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Spell, 273);
                        TaskManager.EnqueueImmediate(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 1286));
                    }
                    else if (ActionManager.Instance()->GetActionStatus(ActionType.Spell, 4087) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Spell, 4087);
                        TaskManager.EnqueueImmediate(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 756));
                    }
                    break;
                case 16:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Spell, 272) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Spell, 272);
                        TaskManager.EnqueueImmediate(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 1286));
                    }
                    else if (ActionManager.Instance()->GetActionStatus(ActionType.Spell, 4073) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Spell, 4073);
                        TaskManager.EnqueueImmediate(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 756));
                    }
                    break;
            }
        }

        private void Use500GPSkill()
        {
            switch (Svc.ClientState.LocalPlayer.ClassJob.Id)
            {
                case 17:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Spell, 224) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Spell, 224);
                        TaskManager.EnqueueImmediate(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 219));
                    }
                    break;
                case 16:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Spell, 241) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Spell, 241);
                        TaskManager.EnqueueImmediate(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 219));
                    }
                    break;
            }

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

                if (item != lastGatheredItem)
                {
                    TaskManager.Abort();
                    lastGatheredIndex = index;
                    lastGatheredItem = item;
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
                    TaskManager.Enqueue(() =>
                    {
                        if (Config.Use100GPYield)
                        {
                            TaskManager.EnqueueImmediate(() => Use100GPSkill());
                        }
                    });
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
