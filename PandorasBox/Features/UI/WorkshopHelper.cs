using ClickLib.Clicks;
using Dalamud;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Logging;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using PandorasBox.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using static ECommons.GenericHelpers;

// TODO:
// prevent schedule from executing if workshop has anything filled in

namespace PandorasBox.Features.UI
{
    public unsafe class WorkshopHelper : Feature
    {
        public override string Name => "Workshop Helper";

        public override string Description => "Adds a menu to the Island Sanctuary workshop to allow quick setting your daily schedules. Supports importing from Overseas Casuals.";

        public override FeatureType FeatureType => FeatureType.UI;
        private Overlays overlay;
        public static bool ResetPosition = false;
        public static bool enabled;

        public Configs Config { get; private set; }
        public override bool UseAutoConfig => true;
        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Execution Delay (ms)", "", 1, IntMin = 0, IntMax = 1000, EditorSize = 300)]
            public int taskDelay = 200;
            [FeatureConfigOption("Automatically go to the next day's cycle when opening the workshop menu.", "", 2)]
            public bool OpenNextDay = false;
        }

        internal static (uint Key, string Name, ushort CraftingTime, ushort LevelReq)[] Craftables;
        public static List<Item> PrimarySchedule = new();
        public static List<Item> SecondarySchedule = new();
        public static List<CyclePreset> MultiCycleList = new();

        private Dictionary<int, bool> Workshops = new Dictionary<int, bool> { [0] = false, [1] = false, [2] = false, [3] = false };
        private int currentWorkshop;
        private int maxWorkshops = 4;
        private List<int> Cycles { get; set; } = new() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };
        private int selectedCycle = 0;
        private bool isScheduleRest;
        private bool overrideRest;
        private bool autoWorkshopSelect = true;
        private bool fortuneteller;
        private bool weekend;
        private bool executionDisabled;
        private bool hasOpened;
        private int currentDay;
        private readonly int taskDelay = 100;

        public class CyclePreset
        {
            public List<Item> PrimarySchedule { get; set; }
            public List<Item> SecondarySchedule { get; set; }
        }

        public class Item
        {
            public uint Key { get; set; }
            public string Name { get; set; }
            public ushort CraftingTime { get; set; }
            public uint UIIndex { get; set; }
            public ushort LevelReq { get; set; }
            public bool InsufficientRank { get; set; }
            public bool OnRestDay { get; set; }
        }

        public override void Draw()
        {
            if (enabled)
            {
                var workshopWindow = Svc.GameGui.GetAddonByName("MJICraftSchedule", 1);
                if (workshopWindow == IntPtr.Zero)
                {
                    hasOpened = false;
                    return;
                }
                var addonPtr = (AtkUnitBase*)workshopWindow;
                if (addonPtr == null)
                    return;

                if (!hasOpened && Config.OpenNextDay && IsAddonReady(addonPtr))
                {
                    OpenCycle(MJIManager.Instance()->CurrentCycleDay + 2);
                    hasOpened = true;
                }

                var baseX = addonPtr->X;
                var baseY = addonPtr->Y;

                if (addonPtr->UldManager.NodeListCount > 1)
                {
                    if (addonPtr->UldManager.NodeList[1]->IsVisible)
                    {
                        var node = addonPtr->UldManager.NodeList[1];

                        if (!node->IsVisible)
                            return;

                        var position = GetNodePosition(node);
                        var scale = GetNodeScale(node);
                        var size = new Vector2(node->Width, node->Height) * scale;
                        var center = new Vector2((position.X + size.X) / 2, (position.Y - size.Y) / 2);
                        //position += ImGuiHelpers.MainViewport.Pos;

                        ImGuiHelpers.ForceNextWindowMainViewport();

                        if ((ResetPosition && position.X != 0))
                        {
                            ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(position.X + size.X + 7, position.Y + 7), ImGuiCond.Always);
                            ResetPosition = false;
                        }
                        else
                        {
                            ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(position.X + size.X + 7, position.Y + 7), ImGuiCond.FirstUseEver);
                        }

                        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(7f, 7f));
                        ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(200f, 200f));
                        ImGui.Begin($"###Options{node->NodeID}", ImGuiWindowFlags.NoScrollbar
                            | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.AlwaysUseWindowPadding);

                        DrawWindowContents();

                        ImGui.End();
                        ImGui.PopStyleVar(2);
                    }
                }
            }
        }

        private void DrawWindowContents()
        {
            if (ImGui.Button("Overseas Casuals Import"))
            {
                try
                {
                    MultiCycleList.Clear();

                    var text = ImGui.GetClipboardText();
                    if (text.IsNullOrEmpty()) return;

                    var rawItemStrings = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    ScheduleImport(rawItemStrings);

                    if ((PrimarySchedule.Count == 0 && !fortuneteller && !weekend) || (MultiCycleList.All(x => x.PrimarySchedule.Count == 0)) && (fortuneteller || weekend))
                        PrintPluginMessage("Failed to parse any items from clipboard. Refer to help icon for how to import.");
                }
                catch (Exception e)
                {
                    PrintPluginMessage("Failed to parse any items from clipboard. Refer to help icon for how to import.");
                    PluginLog.Error($"Could not parse clipboard. Clipboard may be empty.\n{e}");
                }
            }
            ImGuiComponents.HelpMarker("This is for importing schedules from the Overseas Casuals' Discord from your clipboard.\n" +
                "This importer detects the presence of an item's name (not including \"Isleworks\" et al) on each line.\n" +
                "You can copy an entire workshop's schedule from the discord, junk included.\n" +
                "If you want to import the entire day's schedule for all workshops, tick 'Multi-Workshhop Import' checkbox below.");

            if (!fortuneteller)
            {
                ImGui.Checkbox("Weekend Import", ref weekend);
                ImGuiComponents.HelpMarker("This is for importing cycles 5, 6, and 7 at once.");
            }

            if (!weekend)
            {
                ImGui.Checkbox("Fortuneteller Import", ref fortuneteller);
                ImGuiComponents.HelpMarker("This is for importing cycles 3-7.\nCycle 2 will automatically be set to rest.");
            }

            if (!fortuneteller && !weekend)
            {
                if (ImGui.RadioButton($"Auto-Select Workshops", autoWorkshopSelect))
                {
                    autoWorkshopSelect = true;
                }
                ImGuiComponents.HelpMarker("If the cumulative hours in clipboard is <24, this will apply to schedule to all workshops.\n" +
                    "If it is >24, this will apply the first 24hrs of items to workshops 1-3, and the remaining to workshop 4.");
                if (ImGui.RadioButton($"Manually Select Workshops", !autoWorkshopSelect))
                {
                    autoWorkshopSelect = false;
                }
                ImGuiComponents.HelpMarker("For importing one workshop's worth of items at a time and allows you to select which workshops the schedule will apply to.");
            }


            // eventual plan to add entries manually, rearrange and edit existing ones (to fix any invalid entries)
            // basically the same buttons from ReAction's stack tab

            // ImGui.SameLine();
            // ImGui.PushStyleColor(ImGuiCol.Button, ImGui.ColorConvertFloat4ToU32(ImGui.ColorConvertHSVtoRGB(0 / 7.0f, 0.6f, 0.6f)));
            // ImGui.PushStyleColor(ImGuiCol.Button, 0xFF000000);
            // ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xDD000000);
            // ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xAA000000);
            // using (var font = ImRaii.PushFont(UiBuilder.IconFont))
            // {
            //     if (ImGui.Button(FontAwesomeIcon.Times.ToIconString())) { items = null; }
            // }
            // ImGui.PopStyleColor(3);

            ImGui.Text("Import Preview");
            if (!fortuneteller && !weekend)
            {
                if (autoWorkshopSelect && SecondarySchedule.Count == 0)
                {
                    DrawWorkshopListBox($"Workshops 1-4", PrimarySchedule);
                }
                else if (autoWorkshopSelect && SecondarySchedule.Count > 0)
                {
                    DrawWorkshopListBox($"Workshops 1-3", PrimarySchedule);
                    DrawWorkshopListBox($"Workshop 4", SecondarySchedule);
                }
                else
                    DrawWorkshopListBox("", PrimarySchedule);
            }
            else
            {
                ImGui.BeginChild("ScrollableSection", new Vector2(0, 12 * ImGui.GetTextLineHeightWithSpacing()));
                foreach (var cycle in MultiCycleList)
                {
                    DrawWorkshopListBox($"Cycle {MultiCycleList.IndexOf(cycle) + (weekend ? 5 : 3)} Workshops {(cycle.SecondarySchedule.Count > 0 ? "1-3" : "1-4")}", cycle.PrimarySchedule);
                    if (cycle.SecondarySchedule.Count > 0)
                        DrawWorkshopListBox($"Cycle {MultiCycleList.IndexOf(cycle) + (weekend ? 5 : 3)} Workshop 4", cycle.SecondarySchedule);
                }
                ImGui.EndChild();
            }

            if (!fortuneteller && !weekend)
            {
                ImGui.Text("Select Cycle");
                ImGuiComponents.HelpMarker("Leave blank to execute on open cycle.");
                ImGui.SetNextItemWidth(100);
                var cyclePrev = selectedCycle == 0 ? "" : Cycles[selectedCycle - 1].ToString();
                if (ImGui.BeginCombo("", cyclePrev))
                {
                    if (ImGui.Selectable("", selectedCycle == 0))
                        selectedCycle = 0;
                    foreach (var cycle in Cycles)
                    {
                        var selected = ImGui.Selectable(cycle.ToString(), selectedCycle == cycle);

                        if (selected)
                            selectedCycle = cycle;
                    }
                    ImGui.EndCombo();
                }
                ImGui.SameLine();
                if (ImGui.Button("Set Rest"))
                {
                    TaskManager.Enqueue(() => SetRestDay());
                }
                ImGui.SameLine();
                if (ImGui.Button("Prev"))
                {
                    OpenCycle(selectedCycle - 1);
                    selectedCycle = selectedCycle == 0 ? 0 : selectedCycle - 1;
                }
                ImGui.SameLine();
                if (ImGui.Button("Next"))
                {
                    OpenCycle(selectedCycle + 1);
                    selectedCycle = selectedCycle == 14 ? 14 : selectedCycle + 1;
                }

                if (autoWorkshopSelect)
                    ImGui.BeginDisabled();
                ImGui.Text("Select Workshops");
                var currentRank = MJIManager.Instance()->IslandState.CurrentRank;
                for (var i = 0; i < Workshops.Count; i++)
                {
                    if (!IsWorkshopUnlocked(i + 1))
                        ImGui.BeginDisabled();
                    var configValue = Workshops[i];
                    if (ImGui.Checkbox($"{i + 1}", ref configValue)) { Workshops[i] = configValue; }
                    if (i != Workshops.Count - 1) ImGui.SameLine();
                    if (!IsWorkshopUnlocked(i + 1))
                        ImGui.EndDisabled();
                }
                ImGui.SameLine();
                if (ImGui.Button("Deselect"))
                    Workshops.Keys.ToList().ForEach(key => Workshops[key] = false);
                if (autoWorkshopSelect)
                    ImGui.EndDisabled();
            }

            try
            {
                var IsInsufficientRank = (PrimarySchedule.Count > 0 && PrimarySchedule.Any(x => x.InsufficientRank))
                    || (SecondarySchedule.Count > 0 && SecondarySchedule.Any(x => x.InsufficientRank));
                var ScheduleInProgress = selectedCycle - 1 <= MJIManager.Instance()->CurrentCycleDay && selectedCycle != 0;
                var restDays = GetCurrentRestDays();
                var SelectedIsRest = restDays.Contains(selectedCycle - 1);
                if (IsInsufficientRank || ScheduleInProgress || SelectedIsRest)
                {
                    ImGui.BeginDisabled();
                    executionDisabled = true;
                    if (IsInsufficientRank)
                        ImGui.TextColored(ImGuiColors.DalamudRed, "Insufficient rank to execute schedule");
                    if (SelectedIsRest)
                        ImGui.TextColored(ImGuiColors.DalamudRed, "Selected cycle is a rest day.\nCannot schedule on a rest day.");
                    if (ScheduleInProgress)
                        ImGui.TextColored(ImGuiColors.DalamudRed, "Cannot execute schedule on days\nin progress or passed");
                }

                var ScheduleContainsRest = MultiCycleList.Any(x => x.PrimarySchedule.Any(y => y.OnRestDay == true));
                if (ScheduleContainsRest)
                {
                    ImGui.TextColored(ImGuiColors.TankBlue, "Blue cycle will be set to rest");
                    ImGui.Checkbox("Override Rest?", ref overrideRest);
                }
                if (ImGui.Button("Execute Schedule"))
                {
                    currentWorkshop = Workshops.FirstOrDefault(pair => pair.Value).Key;
                    currentDay = fortuneteller ? 3 : 9;
                    if (fortuneteller || weekend)
                        ScheduleMultiCycleList();
                    else
                        ScheduleList();
                }
                if (executionDisabled)
                    ImGui.EndDisabled();
            }
            catch (Exception e) { PluginLog.Log(e.ToString()); return; }
        }

        private bool IsWorkshopUnlocked(int w)
        {
            var currentRank = MJIManager.Instance()->IslandState.CurrentRank;
            switch (w)
            {
                case 1:
                    if (currentRank < 3)
                    {
                        maxWorkshops = 0;
                        return false;
                    }
                    break;
                case 2:
                    if (currentRank < 6)
                    {
                        maxWorkshops = 1;
                        return false;
                    }
                    break;
                case 3:
                    if (currentRank < 8)
                    {
                        maxWorkshops = 2;
                        return false;
                    }
                    break;
                case 4:
                    if (currentRank < 14)
                    {
                        maxWorkshops = 3;
                        return false;
                    }
                    break;
            }
            return true;
        }

        private static void DrawWorkshopListBox(string text, List<Item> schedule)
        {
            var colour = false;

            if (!text.IsNullOrEmpty())
                ImGui.Text(text);

            ImGui.SetNextItemWidth(250);
            if (ImGui.BeginListBox($"##Listbox{text}", new Vector2(250, (schedule.Count > 0 ? schedule.Count + 1 : 3) * ImGui.GetTextLineHeightWithSpacing())))
            {
                if (schedule.Count > 0)
                {
                    foreach (var item in schedule)
                    {
                        if (item.OnRestDay || item.InsufficientRank)
                        {
                            colour = true;

                            if (item.OnRestDay)
                                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.TankBlue);
                            if (item.InsufficientRank)
                                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
                        }

                        ImGui.Text(item.Name);

                        if (colour)
                            ImGui.PopStyleColor();
                    }
                }
                ImGui.EndListBox();
            }
        }

        public static unsafe Vector2 GetNodePosition(AtkResNode* node)
        {
            var pos = new Vector2(node->X, node->Y);
            var par = node->ParentNode;
            while (par != null)
            {
                pos *= new Vector2(par->ScaleX, par->ScaleY);
                pos += new Vector2(par->X, par->Y);
                par = par->ParentNode;
            }

            return pos;
        }

        public static unsafe Vector2 GetNodeScale(AtkResNode* node)
        {
            if (node == null) return new Vector2(1, 1);
            var scale = new Vector2(node->ScaleX, node->ScaleY);
            while (node->ParentNode != null)
            {
                node = node->ParentNode;
                scale *= new Vector2(node->ScaleX, node->ScaleY);
            }

            return scale;
        }

        internal void ScheduleImport(List<string> rawItemStrings)
        {
            if (!fortuneteller && !weekend)
            {
                (var items, var excessItems) = ParseItems(rawItemStrings);
                PrimarySchedule = items;
                SecondarySchedule = excessItems;
            }
            else
            {
                var rawCycles = SplitCycles(rawItemStrings);

                foreach (var cycle in rawCycles)
                {
                    (var items, var excessItems) = ParseItems(cycle);
                    MultiCycleList.Add(new CyclePreset { PrimarySchedule = items, SecondarySchedule = excessItems });
                }
            }
        }

        public static List<List<string>> SplitCycles(List<string> rawLines)
        {
            var cycles = new List<List<string>>();
            var currentCycle = new List<string>();

            foreach (var line in rawLines)
            {
                if (line.StartsWith("Cycle"))
                {
                    if (currentCycle.Count > 0)
                    {
                        currentCycle[currentCycle.Count - 1] += " " + line; // Append "Cycle" line to the previous list
                    }
                }
                else
                    currentCycle.Add(line);
            }
            if (currentCycle.Count > 0)
                cycles.Add(currentCycle);

            return cycles;
        }

        public static (List<Item>, List<Item>) ParseItems(List<string> itemStrings)
        {
            var items = new List<Item>();
            var excessItems = new List<Item>();
            var hours = 0;
            var isRest = false;
            foreach (var itemString in itemStrings)
            {
                if (itemString.ToLower().Contains("rest"))
                    isRest = true;

                var matchFound = false;
                foreach (var craftable in Craftables)
                {
                    if (IsMatch(itemString.ToLower(), craftable.Name.ToLower()))
                    {
                        var item = new Item
                        {
                            Key = craftable.Key,
                            Name = Svc.Data.GetExcelSheet<MJICraftworksObject>().GetRow(craftable.Key).Item.Value.Name.RawString,
                            CraftingTime = craftable.CraftingTime,
                            UIIndex = craftable.Key - 1,
                            LevelReq = craftable.LevelReq,
                            OnRestDay = isRest
                        };
                        item.InsufficientRank = !isCraftworkObjectCraftable(item);

                        if (hours < 24)
                            items.Add(item);
                        else
                            excessItems.Add(item);

                        hours += craftable.CraftingTime;
                        matchFound = true;
                    }
                }
                if (!matchFound)
                {
                    PluginLog.Log($"Failed to match string to craftable: {itemString}");
                    var invalidItem = new Item
                    {
                        Key = 0,
                        Name = "Invalid",
                        CraftingTime = 0,
                        UIIndex = 0,
                        LevelReq = 0
                    };
                    // items.Add(invalidItem);
                }
            }

            return (items, excessItems);
        }

        private static bool IsMatch(string x, string y)
        {
            var pattern = $@"\b{Regex.Escape(y)}\b";
            return Regex.IsMatch(x, pattern);
        }

        private static bool isCraftworkObjectCraftable(Item item) => MJIManager.Instance()->IslandState.CurrentRank >= item.LevelReq;

        private static bool isWorkshopOpen() => Svc.GameGui.GetAddonByName("MJICraftSchedule") != IntPtr.Zero;

        private unsafe bool OpenCycle(int cycle_day)
        {
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("MJICraftSchedule");
            if (!isWorkshopOpen() || !GenericHelpers.IsAddonReady(addon)) return false;

            try
            {
                var workshopPTR = Svc.GameGui.GetAddonByName("MJICraftSchedule");
                if (workshopPTR == IntPtr.Zero)
                    return false;

                var workshopWindow = (AtkUnitBase*)workshopPTR;
                if (workshopWindow == null)
                    return false;

                Callback.Fire(workshopWindow, false, 19, (uint)(cycle_day - 1));

                return true;
            }
            catch
            {
                return false;
            }
        }

        private unsafe bool OpenAgenda(uint index, int workshop, int prevHours)
        {
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("MJICraftSchedule");
            if (!isWorkshopOpen() || !GenericHelpers.IsAddonReady(addon)) return false;

            try
            {
                var workshopPTR = Svc.GameGui.GetAddonByName("MJICraftSchedule");
                if (workshopPTR == IntPtr.Zero)
                    return false;

                var workshopWindow = (AtkUnitBase*)workshopPTR;
                if (workshopWindow == null)
                    return false;


                Callback.Fire(workshopWindow, false, 16, (uint)(workshop), (uint)(index == 0 ? 0 : prevHours));

                return true;
            }
            catch
            {
                return false;
            }
        }

        private unsafe bool ScheduleItem(Item item)
        {
            var addon = Svc.GameGui.GetAddonByName("MJICraftSchedule");
            if (addon == IntPtr.Zero)
                return false;
            if (!GenericHelpers.IsAddonReady((AtkUnitBase*)addon)) return false;

            try
            {
                var schedulerPTR = Svc.GameGui.GetAddonByName("MJICraftScheduleSetting");
                if (schedulerPTR == IntPtr.Zero)
                    return false;
                var schedulerWindow = (AtkUnitBase*)schedulerPTR;
                if (schedulerWindow == null)
                    return false;

                Callback.Fire(schedulerWindow, false, 11, item.UIIndex);
                Callback.Fire(schedulerWindow, false, 13);
                schedulerWindow->Close(true);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private List<int> GetCurrentRestDays()
        {
            var restDays1 = MJIManager.Instance()->CraftworksRestDays[0];
            var restDays2 = MJIManager.Instance()->CraftworksRestDays[1];
            var restDays3 = MJIManager.Instance()->CraftworksRestDays[2];
            var restDays4 = MJIManager.Instance()->CraftworksRestDays[3];
            return new List<int> { restDays1, restDays2, restDays3, restDays4 };
        }

        private bool SetRestDay()
        {
            var addon = Svc.GameGui.GetAddonByName("MJICraftSchedule");
            if (addon == IntPtr.Zero)
                return false;
            if (!GenericHelpers.IsAddonReady((AtkUnitBase*)addon)) return false;

            try
            {
                // open rest days addon
                Callback.Fire((AtkUnitBase*)addon, false, 12);

                var restDaysPTR = Svc.GameGui.GetAddonByName("MJICraftScheduleMaintenance");
                if (restDaysPTR == IntPtr.Zero)
                    return false;
                var schedulerWindow = (AtkUnitBase*)restDaysPTR;
                if (schedulerWindow == null)
                    return false;

                var restDays = GetCurrentRestDays();
                if (selectedCycle <= 6)
                    restDays[1] = selectedCycle - 1;
                else if (selectedCycle >= 7)
                    restDays[3] = selectedCycle - 1;
                var restDaysMask = restDays.Sum(n => (int)Math.Pow(2, n));
                Callback.Fire(schedulerWindow, false, 11, (uint)restDaysMask);

                TaskManager.Enqueue(() => ConfirmYesNo());

                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static bool ConfirmYesNo()
        {
            var mjiRest = (AtkUnitBase*)Svc.GameGui.GetAddonByName("MJICraftScheduleMaintenance");
            if (mjiRest == null) return false;

            if (mjiRest->IsVisible && TryGetAddonByName<AddonSelectYesno>("SelectYesno", out var addon) &&
                addon->AtkUnitBase.IsVisible &&
                addon->YesButton->IsEnabled &&
                addon->AtkUnitBase.UldManager.NodeList[15]->IsVisible)
            {
                new ClickSelectYesNo((IntPtr)addon).Yes();
                return true;
            }

            return false;
        }

        public bool ScheduleList()
        {
            if (!fortuneteller && !weekend)
            {
                if (selectedCycle != 0)
                {
                    TaskManager.EnqueueImmediate(() => OpenCycle(selectedCycle), $"MOpenCycle{selectedCycle}");
                }
            }

            if (isScheduleRest)
            {
                TaskManager.EnqueueImmediate(() => SetRestDay(), $"SetRest");
                return true;
            }

            var hours = 0;
            if (autoWorkshopSelect || fortuneteller || weekend)
            {
                if (SecondarySchedule.Count > 0)
                {
                    for (var i = 0; i < maxWorkshops - 1; i++)
                    {
                        var ws = 0;
                        TaskManager.EnqueueImmediate(() => hours = 0, $"PSSetHours0");
                        foreach (var item in PrimarySchedule)
                        {
                            ws = i;
                            TaskManager.DelayNextImmediate("PSOpenAgendaDelay", Config.taskDelay);
                            TaskManager.EnqueueImmediate(() => OpenAgenda(item.UIIndex, ws, hours), $"PSOpenAgendaW{ws + 1}");
                            TaskManager.DelayNextImmediate("PSScheduleItemDelay", Config.taskDelay);
                            TaskManager.EnqueueImmediate(() => ScheduleItem(item), $"PSScheduleItemW{ws + 1}");
                            TaskManager.EnqueueImmediate(() => hours += item.CraftingTime, $"PSIncrementHoursW{ws + 1}");
                        }
                    }
                    TaskManager.EnqueueImmediate(() => hours = 0, $"SSSetHours0");
                    foreach (var item in SecondarySchedule)
                    {
                        TaskManager.DelayNextImmediate("SSOpenAgendaDelay", Config.taskDelay);
                        TaskManager.EnqueueImmediate(() => OpenAgenda(item.UIIndex, 3, hours), $"SSOpenAgendaW{maxWorkshops}");
                        TaskManager.DelayNextImmediate("SSScheduleItemDelay", Config.taskDelay);
                        TaskManager.EnqueueImmediate(() => ScheduleItem(item), $"SSScheduleW{maxWorkshops}");
                        TaskManager.EnqueueImmediate(() => hours += item.CraftingTime, $"SSIncrementHoursW{maxWorkshops}");
                    }
                }
                else
                {
                    for (var i = 0; i < maxWorkshops; i++)
                    {
                        var ws = 0;
                        TaskManager.EnqueueImmediate(() => hours = 0, $"PSOSetHours0");
                        foreach (var item in PrimarySchedule)
                        {
                            ws = i;
                            TaskManager.DelayNextImmediate("PSOOpenAgendaDelay", Config.taskDelay);
                            TaskManager.EnqueueImmediate(() => OpenAgenda(item.UIIndex, ws, hours), $"PSOOpenAgendaW{ws + 1}");
                            TaskManager.DelayNextImmediate("PSOScheduleItemDelay", Config.taskDelay);
                            TaskManager.EnqueueImmediate(() => ScheduleItem(item), $"PSOScheduleW{ws + 1}");
                            TaskManager.EnqueueImmediate(() => hours += item.CraftingTime, $"PSOIncrementHoursW{ws + 1}");
                        }
                    }
                }
            }
            else
            {
                for (var i = currentWorkshop; i < Workshops.Count; i++)
                {
                    var ws = 0;
                    if (Workshops[i])
                    {
                        TaskManager.EnqueueImmediate(() => hours = 0, $"MSetHours0");
                        foreach (var item in PrimarySchedule)
                        {
                            ws = i;
                            TaskManager.DelayNextImmediate("MOpenAgendaDelay", Config.taskDelay);
                            TaskManager.EnqueueImmediate(() => OpenAgenda(item.UIIndex, ws, hours), $"MOpenAgendaW{ws + 1}");
                            TaskManager.DelayNextImmediate("MScheduleItemDelay", Config.taskDelay);
                            TaskManager.EnqueueImmediate(() => ScheduleItem(item), $"MScheduleW{ws + 1}");
                            TaskManager.EnqueueImmediate(() => hours += item.CraftingTime, $"MIncrementHoursW{ws + 1}");
                            TaskManager.EnqueueImmediate(() => currentWorkshop += 1, $"MIncrementWFromW{ws + 1}");
                        }
                    }
                }
                TaskManager.EnqueueImmediate(() => currentWorkshop = 0, $"MSetWorkshop0");
            }
            return true;
        }

        public void ScheduleMultiCycleList()
        {
            if (fortuneteller)
            {
                TaskManager.Enqueue(() => OpenCycle(2), "FortuneTellerCycleOpen");
                TaskManager.Enqueue(() => SetRestDay(), "FortuneTellerCycleSetRestDay");
            }
            TaskManager.Enqueue(() =>
            {
                foreach (var cycle in MultiCycleList)
                {
                    TaskManager.Enqueue(() => OpenCycle(currentDay), $"MultiCycleOpenCycleOn{currentDay}");
                    TaskManager.Enqueue(() => PrimarySchedule = cycle.PrimarySchedule, $"MultiCycleSetPrimaryCycleOn{currentDay}");
                    TaskManager.Enqueue(() => SecondarySchedule = cycle.SecondarySchedule, $"MultiCyleSetSecondaryCycleOn{currentDay}");
                    TaskManager.Enqueue(() => ScheduleList(), $"MultiCycleScheduleListOn{currentDay}");
                    TaskManager.Enqueue(() => { isScheduleRest = overrideRest ? false : PrimarySchedule[0].OnRestDay; }, $"MultiCycleCheckRestOn{currentDay}");
                    TaskManager.Enqueue(() => currentDay += 1, $"MultiCycleScheduleIncrementDayFrom{currentDay}");
                }
            }, "ScheduleMultiCycleForEach");
        }
        private void CheckIfInvalidSchedule(ref SeString message, ref bool isHandled)
        {
            // Unable to set agenda. Insufficient time for handicraft production.
            if (message.ExtractText() == Svc.Data.GetExcelSheet<LogMessage>().First(x => x.RowId == 10146).Text.ExtractText())
            {
                TaskManager.Abort();
                if (WorkshopsRemaining())
                {
                    TaskManager.Enqueue(() => currentWorkshop += 1);
                    ScheduleList();
                }
            }
        }

        private bool WorkshopsRemaining()
        {
            return Workshops.Skip(currentWorkshop).Any(pair => pair.Value);
        }

        public void PrintPluginMessage(String msg)
        {
            var message = new XivChatEntry
            {
                Message = new SeStringBuilder()
                .AddUiForeground($"[{P.Name}] ", 45)
                .AddUiForeground($"[{Name}] ", 62)
                .AddText(msg)
                .Build()
            };

            Svc.Chat.PrintChat(message);
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Craftables = Svc.Data.GetExcelSheet<MJICraftworksObject>()
                .Where(x => x.Item.Row > 0)
                .Select(x => (x.RowId, x.Item.GetDifferentLanguage(ClientLanguage.English).Value.Name.RawString.Replace("Isleworks", "").Replace("Islefish", "").Replace("Isleberry", "").Trim(), x.CraftingTime, x.LevelReq))
                .ToArray();
            overlay = new Overlays(this);
            enabled = true;
            Svc.Toasts.ErrorToast += CheckIfInvalidSchedule;
            base.Enable();
        }

        public override void Disable()
        {
            SaveConfig(Config);
            P.Ws.RemoveWindow(overlay);
            enabled = false;
            Svc.Toasts.ErrorToast -= CheckIfInvalidSchedule;
            base.Disable();
        }
    }
}
