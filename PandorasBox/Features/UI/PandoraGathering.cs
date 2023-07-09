using ClickLib.Bases;
using ClickLib.Structures;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Logging;
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
using System.Collections.Generic;
using System.Drawing;
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

        internal Vector4 DarkTheme = new Vector4(0.26f, 0.26f, 0.26f, 1f);
        internal Vector4 LightTheme = new Vector4(0.97f, 0.87f, 0.75f, 1f);
        internal Vector4 ClassicFFTheme = new Vector4(0.21f, 0f, 0.68f, 1f);
        internal Vector4 LightBlueTheme = new Vector4(0.21f, 0.36f, 0.59f, 0.25f);

        internal int SwingCount = 0;
        public override string Name => "Pandora Quick Gather";

        public override string Description => "Replaces the Quick Gather checkbox with a new one that enables better quick gathering. Works on all nodes and can be interrupted at any point by disabling the checkbox. Also remembers your settings between sessions.";

        public bool InDiadem => Svc.ClientState.TerritoryType == 939;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Hold Shift to Temporarily Disable on Starting a Node", "", 1)]
            public bool ShiftStop = false;

            [FeatureConfigOption("Enable Pandora Gathering", "", 2)]
            public bool Gathering = false;

            [FeatureConfigOption("Remember Item Between Nodes", "", 3)]
            public bool RememberLastNode = false;

            [FeatureConfigOption("Use King's Yield II / Blessed Harvest II", "", 4)]
            public bool Use500GPYield = false;

            [FeatureConfigOption("Use Bountiful Yield / Bountiful Harvest", "", 5)]
            public bool Use100GPYield = false;

            [FeatureConfigOption("Use Nald'thal's Tidings / Nophica's Tidings", "", 6)]
            public bool UseTidings = false;

            [FeatureConfigOption("Min. Gatherer's Boon% For Tidings", "", 7, ConditionalDisplay = true, IntMin = 0, IntMax = 100, EditorSize = 300)]
            public int GatherersBoon = 100;

            [FeatureConfigOption("Use The Giving Land", "", 8)]
            public bool UseGivingLand = false;

            [FeatureConfigOption("Use The Twelves Bounty", "", 9)]
            public bool UseTwelvesBounty = false;

            [FeatureConfigOption("Use Solid Reason", "", 10)]
            public bool UseSolidReason = false;

            public bool ShouldShowGatherersBoon() => UseTidings;

        }

        public Configs Config { get; private set; }


        public override FeatureType FeatureType => FeatureType.UI;

        private Overlays Overlay;

        public override bool UseAutoConfig => false;

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

                Svc.GameConfig.TryGet(Dalamud.Game.Config.SystemConfigOption.ColorThemeType, out uint color);

                Vector4 theme = color switch
                {
                    0 => DarkTheme,
                    1 => LightTheme,
                    2 => ClassicFFTheme,
                    3 => LightBlueTheme
                };

                if (color == 3)
                {
                    addon->UldManager.NodeList[5]->ToggleVisibility(false);
                    addon->UldManager.NodeList[6]->ToggleVisibility(false);
                    addon->UldManager.NodeList[9]->ToggleVisibility(false);
                    addon->UldManager.NodeList[8]->ToggleVisibility(false);
                }

                if (color == 1)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0f, 0f, 0f, 1f));
                }

                ImGui.PushStyleColor(ImGuiCol.WindowBg, ImGui.GetColorU32(theme));
                ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGuiColors.DalamudGrey3);
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0f.Scale());
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0f.Scale(), 0f.Scale()));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 1f.Scale());
                ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, size);

                ImGui.GetFont().Scale = scale.X;
                var oldScale = ImGui.GetIO().FontGlobalScale;
                ImGui.GetIO().FontGlobalScale = 0.9f;
                ImGui.PushFont(ImGui.GetFont());
                size.Y *= 6;
                size.X *= 1.065f;
                position.X -= 15f * scale.X;

                ImGuiHelpers.ForceNextWindowMainViewport();
                ImGuiHelpers.SetNextWindowPosRelativeMainViewport(position);
                ImGui.SetNextWindowSize(size);
                ImGui.Begin($"###PandoraGathering{node->NodeID}", ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings
                    | ImGuiWindowFlags.NoResize);

                ImGui.Columns(2, null, false);

                ImGui.Dummy(new Vector2(10));

                if (ImGui.Checkbox("Enable Pandora Gathering", ref Config.Gathering))
                {
                    if (Config.Gathering && node->GetAsAtkComponentCheckBox()->IsChecked)
                        QuickGatherToggle(null);

                    if (!Config.Gathering)
                        TaskManager.Abort();

                    SaveConfig(Config);
                }

                ImGui.NextColumn();

                ImGui.Dummy(new Vector2(10));
                if (ImGui.Checkbox("Remember Item Between Nodes", ref Config.RememberLastNode))
                    SaveConfig(Config);

                if (ImGui.IsItemHovered() && InDiadem)
                {
                    ImGui.BeginTooltip();
                    ImGui.Text("In the Diadem, this will remember the last slot selected and not the last item due to the varying nature of the nodes.");
                    ImGui.EndTooltip();
                }
                var language = Svc.ClientState.ClientLanguage;
                switch (Svc.ClientState.LocalPlayer.ClassJob.Id)
                {
                    case 17:
                        ImGui.NextColumn();
                        if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(4087).Name.RawString}", ref Config.Use100GPYield))
                        {
                            Config.UseGivingLand = false;
                            Config.UseTwelvesBounty = false;
                            SaveConfig(Config);
                        }
                        ImGui.NextColumn();
                        if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(224).Name.RawString}", ref Config.Use500GPYield))
                        {
                            Config.UseGivingLand = false;
                            Config.UseTwelvesBounty = false;
                            SaveConfig(Config);
                        }
                        ImGui.NextColumn();
                        if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(21204).Name.RawString}", ref Config.UseTidings))
                        {
                            Config.UseGivingLand = false;
                            Config.UseTwelvesBounty = false;
                            SaveConfig(Config);
                        }
                        ImGui.NextColumn();
                        if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(215).Name.RawString}", ref Config.UseSolidReason))
                        {
                            SaveConfig(Config);
                        }
                        break;
                    case 16:
                        ImGui.NextColumn();
                        if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(4073).Name.RawString}", ref Config.Use100GPYield))
                        {
                            Config.UseGivingLand = false;
                            Config.UseTwelvesBounty = false;
                            SaveConfig(Config);
                        }
                        ImGui.NextColumn();
                        if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(241).Name.RawString}", ref Config.Use500GPYield))
                        {
                            Config.UseGivingLand = false;
                            Config.UseTwelvesBounty = false;
                            SaveConfig(Config);
                        }
                        ImGui.NextColumn();
                        if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(21203).Name.RawString}", ref Config.UseTidings))
                        {
                            Config.UseGivingLand = false;
                            Config.UseTwelvesBounty = false;
                            SaveConfig(Config);
                        }
                        ImGui.NextColumn();
                        if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(232).Name.RawString}", ref Config.UseSolidReason))
                        {
                            SaveConfig(Config);
                        }
                        break;
                }

                ImGui.NextColumn();
                if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(4590).Name.RawString}", ref Config.UseGivingLand))
                {
                    Config.Use100GPYield = false;
                    Config.Use500GPYield = false;
                    Config.UseTidings = false;
                    SaveConfig(Config);
                }

                ImGui.NextColumn();
                if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(282).Name.RawString.ToTitleCase()}", ref Config.UseTwelvesBounty))
                {
                    Config.Use100GPYield = false;
                    Config.Use500GPYield = false;
                    Config.UseTidings = false;
                    SaveConfig(Config);
                }

                ImGui.Columns(1);
                ImGui.End();

                ImGui.GetFont().Scale = 1;
                ImGui.GetIO().FontGlobalScale = oldScale;
                ImGui.PopFont();

                ImGui.PopStyleVar(4);
                ImGui.PopStyleColor(color == 1 ? 3 : 2);

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
            Svc.Condition.ConditionChange += ResetCounter;

            base.Enable();
        }

        private void ResetCounter(ConditionFlag flag, bool value)
        {
            if (flag == ConditionFlag.Gathering && !value)
            {
                TaskManager.Abort();
                TaskManager.Enqueue(() => SwingCount = 0);
            }
        }

        
        protected override DrawConfigDelegate DrawConfigTree => (ref bool _) =>
        {
            if (ImGui.Checkbox($"Hold Shift to Temporarily Disable on Starting a Node", ref Config.ShiftStop))
                SaveConfig(Config);

            ImGui.Columns(2, null, false);

            ImGui.Dummy(new Vector2(10));

            if (ImGui.Checkbox("Enable Pandora Gathering", ref Config.Gathering))
            {
                if (!Config.Gathering)
                    TaskManager.Abort();

                SaveConfig(Config);
            }

            ImGui.NextColumn();

            ImGui.Dummy(new Vector2(10));
            if (ImGui.Checkbox("Remember Item Between Nodes", ref Config.RememberLastNode))
                SaveConfig(Config);

            if (ImGui.IsItemHovered() && InDiadem)
            {
                ImGui.BeginTooltip();
                ImGui.Text("In the Diadem, this will remember the last slot selected and not the last item due to the varying nature of the nodes.");
                ImGui.EndTooltip();
            }
            var language = Svc.ClientState.ClientLanguage;
            switch (Svc.ClientState.LocalPlayer.ClassJob.Id)
            {
                case 17:
                    ImGui.NextColumn();
                    if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(4087).Name.RawString}", ref Config.Use100GPYield))
                    {
                        Config.UseGivingLand = false;
                        Config.UseTwelvesBounty = false;
                        SaveConfig(Config);
                    }
                    ImGui.NextColumn();
                    if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(224).Name.RawString}", ref Config.Use500GPYield))
                    {
                        Config.UseGivingLand = false;
                        Config.UseTwelvesBounty = false;
                        SaveConfig(Config);
                    }
                    ImGui.NextColumn();
                    if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(21204).Name.RawString}", ref Config.UseTidings))
                    {
                        Config.UseGivingLand = false;
                        Config.UseTwelvesBounty = false;
                        SaveConfig(Config);
                    }
                    ImGui.NextColumn();
                    if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(215).Name.RawString}", ref Config.UseSolidReason))
                    {
                        SaveConfig(Config);
                    }
                    break;
                case 16:
                    ImGui.NextColumn();
                    if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(4073).Name.RawString}", ref Config.Use100GPYield))
                    {
                        Config.UseGivingLand = false;
                        Config.UseTwelvesBounty = false;
                        SaveConfig(Config);
                    }
                    ImGui.NextColumn();
                    if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(241).Name.RawString}", ref Config.Use500GPYield))
                    {
                        Config.UseGivingLand = false;
                        Config.UseTwelvesBounty = false;
                        SaveConfig(Config);
                    }
                    ImGui.NextColumn();
                    if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(21203).Name.RawString}", ref Config.UseTidings))
                    {
                        Config.UseGivingLand = false;
                        Config.UseTwelvesBounty = false;
                        SaveConfig(Config);
                    }
                    ImGui.NextColumn();
                    if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(232).Name.RawString}", ref Config.UseSolidReason))
                    {
                        SaveConfig(Config);
                    }
                    break;
            }

            ImGui.NextColumn();
            if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(4590).Name.RawString}", ref Config.UseGivingLand))
            {
                Config.Use100GPYield = false;
                Config.Use500GPYield = false;
                Config.UseTidings = false;
                SaveConfig(Config);
            }

            ImGui.NextColumn();
            if (ImGui.Checkbox($"Use {Svc.Data.GetExcelSheet<Action>(language).GetRow(282).Name.RawString.ToTitleCase()}", ref Config.UseTwelvesBounty))
            {
                Config.Use100GPYield = false;
                Config.Use500GPYield = false;
                Config.UseTidings = false;
                SaveConfig(Config);
            }

            ImGui.Columns(1);
        };

        private void CheckLastItem(SetupAddonArgs obj)
        {
            if (obj.AddonName == "Gathering" && Config.Gathering && ((Config.ShiftStop && !ImGui.GetIO().KeyShift) || (!Config.ShiftStop)))
            {
                TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Gathering42]);
                TaskManager.Enqueue(() =>
                {
                    var addon = (AddonGathering*)Svc.GameGui.GetAddonByName("Gathering", 1);

                    List<uint> ids = new List<uint>()
                    {
                    addon->GatheredItemId1,
                    addon->GatheredItemId2,
                    addon->GatheredItemId3,
                    addon->GatheredItemId4,
                    addon->GatheredItemId5,
                    addon->GatheredItemId6,
                    addon->GatheredItemId7,
                    addon->GatheredItemId8
                    };

                    if (ids.Any(x => Svc.Data.Excel.GetSheet<EventItem>().Any(y => y.RowId == x)))
                    {
                        Svc.Chat.PrintError($"This node contains quest nodes which can result in soft-locking the quest. Pandora Gathering has been disabled.");
                        this.Disable();
                        return;
                    }

                    Dictionary<int, int> boonChances = new();

                    Int32.TryParse(addon->AtkUnitBase.UldManager.NodeList[25]->GetAsAtkComponentNode()->Component->UldManager.NodeList[21]->GetAsAtkTextNode()->NodeText.ToString(), out int n1b);
                    Int32.TryParse(addon->AtkUnitBase.UldManager.NodeList[24]->GetAsAtkComponentNode()->Component->UldManager.NodeList[21]->GetAsAtkTextNode()->NodeText.ToString(), out int n2b);
                    Int32.TryParse(addon->AtkUnitBase.UldManager.NodeList[23]->GetAsAtkComponentNode()->Component->UldManager.NodeList[21]->GetAsAtkTextNode()->NodeText.ToString(), out int n3b);
                    Int32.TryParse(addon->AtkUnitBase.UldManager.NodeList[22]->GetAsAtkComponentNode()->Component->UldManager.NodeList[21]->GetAsAtkTextNode()->NodeText.ToString(), out int n4b);
                    Int32.TryParse(addon->AtkUnitBase.UldManager.NodeList[21]->GetAsAtkComponentNode()->Component->UldManager.NodeList[21]->GetAsAtkTextNode()->NodeText.ToString(), out int n5b);
                    Int32.TryParse(addon->AtkUnitBase.UldManager.NodeList[20]->GetAsAtkComponentNode()->Component->UldManager.NodeList[21]->GetAsAtkTextNode()->NodeText.ToString(), out int n6b);
                    Int32.TryParse(addon->AtkUnitBase.UldManager.NodeList[19]->GetAsAtkComponentNode()->Component->UldManager.NodeList[21]->GetAsAtkTextNode()->NodeText.ToString(), out int n7b);
                    Int32.TryParse(addon->AtkUnitBase.UldManager.NodeList[18]->GetAsAtkComponentNode()->Component->UldManager.NodeList[21]->GetAsAtkTextNode()->NodeText.ToString(), out int n8b);

                    boonChances.Add(0, n1b);
                    boonChances.Add(1, n2b);
                    boonChances.Add(2, n3b);
                    boonChances.Add(3, n4b);
                    boonChances.Add(4, n5b);
                    boonChances.Add(5, n6b);
                    boonChances.Add(6, n7b);
                    boonChances.Add(7, n8b);

                    if (Config.UseTidings && ((boonChances.TryGetValue((int)lastGatheredIndex, out var val) && val >= Config.GatherersBoon) || boonChances.Where(x => x.Value != 0).All(x => x.Value >= Config.GatherersBoon)))
                    {
                        TaskManager.Enqueue(() => UseTidings(), "UseTidings");
                        TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Gathering42]);
                    }

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

                    if (Config.UseTwelvesBounty)
                    {
                        TaskManager.Enqueue(() => UseTwelvesBounty(), "UseTwelvesSetup");
                        TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Gathering42]);
                    }

                    if (Config.UseGivingLand)
                    {
                        TaskManager.Enqueue(() => UseGivingLand(), "UseGivingSetup");
                        TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Gathering42]);
                    }

                    if (Config.RememberLastNode)
                    {
                        if (lastGatheredIndex > 7)
                            return;

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

                        if (item == lastGatheredItem || InDiadem)
                        {
                            bool quickGathering = addon->QuickGatheringComponentCheckBox->IsChecked;
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
                });
            }
        }

        private bool? UseIntegrityAction()
        {
            switch (Svc.ClientState.LocalPlayer.ClassJob.Id)
            {
                case 17:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Spell, 215) == 0)
                    {
                        SwingCount--;
                        ActionManager.Instance()->UseAction(ActionType.Spell, 215);
                    }
                    break;
                case 16:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Spell, 232) == 0)
                    {
                        SwingCount--;
                        ActionManager.Instance()->UseAction(ActionType.Spell, 232);
                    }
                    break;
            }

            return true;
        }

        private bool? UseGivingLand()
        {
            switch (Svc.ClientState.LocalPlayer.ClassJob.Id)
            {
                case 17:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Spell, 4590) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Spell, 4590);
                        TaskManager.EnqueueImmediate(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 1802));
                    }
                    break;
                case 16:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Spell, 4589) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Spell, 4589);
                        TaskManager.EnqueueImmediate(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 1802));
                    }
                    break;
            }

            return true;
        }

        private bool? UseTwelvesBounty()
        {
            switch (Svc.ClientState.LocalPlayer.ClassJob.Id)
            {
                case 17:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Spell, 282) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Spell, 282);
                        TaskManager.EnqueueImmediate(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 825));
                    }
                    break;
                case 16:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Spell, 280) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Spell, 280);
                        TaskManager.EnqueueImmediate(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 825));
                    }
                    break;
            }

            return true;
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

        private void UseTidings()
        {
            switch (Svc.ClientState.LocalPlayer.ClassJob.Id)
            {
                case 17: //BTN
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Spell, 21204) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Spell, 21204);
                        TaskManager.EnqueueImmediate(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 2667));
                    }
                    break;
                case 16: //MIN
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Spell, 21203) == 0)
                    {
                        ActionManager.Instance()->UseAction(ActionType.Spell, 21203);
                        TaskManager.EnqueueImmediate(() => Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 2667));
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
            SwingCount++;
            PluginLog.Debug($"SWING {SwingCount}");
            var addon = (AddonGathering*)Svc.GameGui.GetAddonByName("Gathering", 1);
            if (addon != null && Config.Gathering)
            {
                List<uint> ids = new List<uint>()
                {
                    addon->GatheredItemId1,
                    addon->GatheredItemId2,
                    addon->GatheredItemId3,
                    addon->GatheredItemId4,
                    addon->GatheredItemId5,
                    addon->GatheredItemId6,
                    addon->GatheredItemId7,
                    addon->GatheredItemId8
                };

                if (ids.Any(x => Svc.Data.Excel.GetSheet<EventItem>().Any(y => y.RowId == x)))
                {
                    Svc.Chat.PrintError($"This node contains quest nodes which can result in soft-locking the quest. Pandora Gathering has been disabled.");
                    this.Disable();
                    return;
                }

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

                if (item != lastGatheredItem && item != 0)
                {
                    TaskManager.Abort();
                    lastGatheredIndex = index;
                    lastGatheredItem = item;
                }

                if (item != 0 && (Svc.Data.GetExcelSheet<Item>().GetRow(item) != null && !Svc.Data.GetExcelSheet<Item>().GetRow(item).IsCollectable) || Svc.Data.GetExcelSheet<Item>().GetRow(item) == null)
                {
                    bool quickGathering = addon->QuickGatheringComponentCheckBox->IsChecked;
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
                        if (Config.UseSolidReason && SwingCount >= 2)
                        {
                            TaskManager.EnqueueImmediate(() => UseIntegrityAction());
                            TaskManager.EnqueueImmediate(() => !Svc.Condition[ConditionFlag.Gathering42]);
                            TaskManager.EnqueueImmediate(() => UseWisdom());
                        }
                    });
                    TaskManager.Enqueue(() =>
                    {
                        if (Config.Use100GPYield)
                        {
                            TaskManager.EnqueueImmediate(() => Use100GPSkill());
                        }
                    });
                    TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Gathering42]);
                    TaskManager.Enqueue(() => eventDelegate.Invoke(&addon->AtkUnitBase.AtkEventListener, ClickLib.Enums.EventType.CHANGE, (uint)index, eventData.Data, inputData.Data));
                }

            }
            gatherEventHook.Original(a1, index, a3, a4);

        }

        private bool? UseWisdom()
        {
            switch (Svc.ClientState.LocalPlayer.ClassJob.Id)
            {
                case 17:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Spell, 26522) == 0)
                    {
                        SwingCount--;
                        ActionManager.Instance()->UseAction(ActionType.Spell, 26522);
                    }
                    break;
                case 16:
                    if (ActionManager.Instance()->GetActionStatus(ActionType.Spell, 26521) == 0)
                    {
                        SwingCount--;
                        ActionManager.Instance()->UseAction(ActionType.Spell, 26521);
                    }
                    break;
            }

            return true;
        }

        public override void Disable()
        {
            P.Ws.RemoveWindow(Overlay);
            SaveConfig(Config);
            gatherEventHook?.Disable();
            quickGatherToggle?.Disable();
            Common.OnAddonSetup -= CheckLastItem;

            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Gathering");
            addon->UldManager.NodeList[5]->ToggleVisibility(true);
            addon->UldManager.NodeList[6]->ToggleVisibility(true);
            addon->UldManager.NodeList[8]->ToggleVisibility(true);
            addon->UldManager.NodeList[9]->ToggleVisibility(true);
            addon->UldManager.NodeList[10]->ToggleVisibility(true);

            Svc.Condition.ConditionChange -= ResetCounter;

            base.Disable();
        }
    }
}
