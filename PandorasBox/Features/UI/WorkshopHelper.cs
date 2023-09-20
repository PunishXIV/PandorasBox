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
using ECommons.ImGuiMethods;
using ECommons.Logging;
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
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using static ECommons.GenericHelpers;

// TODO:
// prevent schedule from executing if workshop has anything filled in
// schedule with rest on second week

namespace PandorasBox.Features.UI
{
    public unsafe class WorkshopHelper : Feature
    {
        public override string Name => "Workshop Helper";

        public override string Description => "Adds a menu to the Island Sanctuary workshop to allow quick setting your daily schedules. Supports importing from Overseas Casuals.";

        public override FeatureType FeatureType => FeatureType.UI;
        private Overlays overlay;
        public static bool ResetPosition = false;

        public Configs Config { get; private set; }
        public override bool UseAutoConfig => true;
        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Execution Delay (ms)", "", 1, IntMin = 0, IntMax = 1000, EditorSize = 300)]
            public int taskDelay = 200;
            [FeatureConfigOption("Delay After Switching Cycles (ms)", "", 2, IntMin = 0, IntMax = 1000, EditorSize = 300)]
            public int taskAfterCycleSwitchDelay = 500;
            [FeatureConfigOption("Automatically go to the next day's cycle when opening the workshop menu.", "", 3)]
            public bool OpenNextDay = false;
            [FeatureConfigOption("Automatically import from clipboard when loading the workshop.", "", 4)]
            public bool AutoImport = false;

            [FeatureConfigOption("Hide the Taskmaster. Useful for EXPlosion.", "", 5)]
            public bool HideTaskmaster = false;

            [FeatureConfigOption("Automatically export materials when speaking with the export mammet.", "", 6)]
            public bool AutoSell = false;
            public bool ShouldShowAutoSellAmount() => AutoSell;
            [FeatureConfigOption("Auto Sell Above", "", 7, IntMin = 0, IntMax = 999, EditorSize = 300, ConditionalDisplay = true)]
            public int AutoSellAmount = 900;

            [FeatureConfigOption("Automatically collect drops from pasture", "", 8)]
            public bool AutoCollectPasture = false;
            [FeatureConfigOption("Automatically collect crops from farm", "", 9)]
            public bool AutoCollectFarm = false;

            [FeatureConfigOption("Auto Collect Granary", "", 10)]
            public bool AutoCollectGranary = false;
            //[FeatureConfigOption("Auto Set Granary", "", 11)]
            //public bool AutoSetGranary = false;
            //public bool ShouldShowAutoConfirmGranary() => AutoSetGranary;
            //[FeatureConfigOption("Auto Confirm Granary", "", 12, ConditionalDisplay = true)]
            //public bool AutoConfirmGranary = false;
            [FeatureConfigOption("Auto Max Granary", "", 13)]
            public bool AutoMaxGranary = false;
        }

        internal static (uint Key, string Name, ushort CraftingTime, ushort LevelReq)[] Craftables;
        public static List<Item> PrimarySchedule = new();
        public static List<Item> SecondarySchedule = new();
        public static List<CyclePreset> MultiCycleList = new();

        internal Dictionary<int, bool> Workshops = new() { [0] = false, [1] = false, [2] = false, [3] = false };
        private int currentWorkshop;
        private int maxWorkshops = 4;
        private List<int> Cycles { get; set; } = new() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };
        private int selectedCycle = 0;
        private bool isScheduleRest;
        private bool overrideRest;
        private bool autoWorkshopSelect = true;
        private bool overrideExecutionDisable;
        private int currentDay;

        internal const int weekendOffset = 5;
        internal const int fortuneOffset = 3;
        internal const int nextDayOffset = 2;

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
            var workshopWindow = Svc.GameGui.GetAddonByName("MJICraftSchedule", 1);
            if (workshopWindow == IntPtr.Zero)
            {
                PrimarySchedule.Clear();
                SecondarySchedule.Clear();
                MultiCycleList.Clear();
                currentWorkshop = 0;
                maxWorkshops = 4;
                selectedCycle = 0;
                isScheduleRest = false;
                overrideRest = false;
                autoWorkshopSelect = true;
                overrideExecutionDisable = false;
                currentDay = 0;
                return;
            }
            var addonPtr = (AtkUnitBase*)workshopWindow;
            if (addonPtr == null)
                return;

            if (addonPtr->UldManager.NodeListCount > 1)
            {
                if (addonPtr->UldManager.NodeList[1]->IsVisible)
                {
                    var node = addonPtr->UldManager.NodeList[1];

                    if (!node->IsVisible)
                        return;

                    var position = AtkResNodeHelper.GetNodePosition(node);
                    var scale = AtkResNodeHelper.GetNodeScale(node);
                    var size = new Vector2(node->Width, node->Height) * scale;
                    var center = new Vector2((position.X + size.X) / 2, (position.Y - size.Y) / 2);

                    ImGuiHelpers.ForceNextWindowMainViewport();
                    ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(position.X + size.X + 7, position.Y + 7), ImGuiCond.Always);

                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(7f, 7f));
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(200f, 200f));
                    ImGui.Begin($"###WorkshopHelper", ImGuiWindowFlags.NoScrollbar
                        | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.AlwaysUseWindowPadding);

                    DrawWindowContents();

                    ImGui.End();
                    ImGui.PopStyleVar(2);
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

                    if (MultiCycleList.All(x => x.PrimarySchedule.Count == 0))
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
                "You can copy an entire workshop's schedule from the discord, junk included.");

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

            ImGui.Text("Import Preview");

            ImGui.BeginChild("ScrollableSection", new Vector2(0, (!autoWorkshopSelect || MultiCycleList.All(x => x.PrimarySchedule.Count == 0) || (MultiCycleList.Count == 1 && MultiCycleList[0].SecondarySchedule.Count == 0) ? 6 : 12) * ImGui.GetTextLineHeightWithSpacing()));

            // if I want to actually use this code I need to take into account rest overrides and the outcome of a schedule being executed

            //var initialCycleNum = selectedCycle == 0 ? MJIManager.Instance()->CurrentCycleDay + nextDayOffset : selectedCycle;
            //var adjustedCycleNums = MultiCycleList.Select((cycle, index) =>
            //{
            //    var cycleNum = initialCycleNum + index;
            //    var daysToAdd = 0;
            //    while (GetCurrentRestDays().Any(x => x == cycleNum - 1 + daysToAdd))
            //        daysToAdd++;
            //    return cycleNum + daysToAdd;
            //}).ToList();

            foreach (var cycle in MultiCycleList)
            {
                if (MultiCycleList.IndexOf(cycle) > 0 && !autoWorkshopSelect)
                    continue;

                var cycleNum = MultiCycleList.IndexOf(cycle)
                    + (selectedCycle == 0 ? MJIManager.Instance()->CurrentCycleDay + nextDayOffset
                    : selectedCycle);

                //var cycleNum = adjustedCycleNums[MultiCycleList.IndexOf(cycle)];

                DrawWorkshopListBox($"Cycle {cycleNum} Workshops {(!autoWorkshopSelect ? string.Join(", ", Workshops.Where(x => x.Value).Select(x => x.Key + 1)) : (cycle.SecondarySchedule.Count > 0 ? "1-3" : "1-4"))}", cycle.PrimarySchedule);

                if (cycle.SecondarySchedule.Count > 0 && autoWorkshopSelect)
                    DrawWorkshopListBox($"Cycle {cycleNum} Workshop 4", cycle.SecondarySchedule);
            }
            ImGui.EndChild();

            ImGui.Text("Select Cycle");

            ImGuiComponents.HelpMarker("Leave blank to execute on next available day.");

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

            var SelectedUnavailable = selectedCycle - 1 == MJIManager.Instance()->CurrentCycleDay || MJIManager.Instance()->CraftworksRestDays[1] <= MJIManager.Instance()->CurrentCycleDay;
            if (SelectedUnavailable)
                ImGui.BeginDisabled();

            if (ImGui.Button("Set Rest"))
            {
                TaskManager.Enqueue(() => SetRestDay(selectedCycle));
            }

            if (SelectedUnavailable)
                ImGui.EndDisabled();

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

            try
            {
                ImGui.Text("Select Workshops");

                if (!autoWorkshopSelect)
                {
                    for (var i = 0; i < Workshops.Count; i++)
                    {
                        if (!IsWorkshopUnlocked(i + 1))
                            ImGui.BeginDisabled();

                        try
                        {
                            var configValue = Workshops[i];
                            if (ImGui.Checkbox($"{i + 1}", ref configValue)) { Workshops[i] = configValue; }
                            if (i != Workshops.Count - 1) ImGui.SameLine();
                        }
                        catch (Exception ex)
                        {
                            ex.Log();
                        }

                        if (!IsWorkshopUnlocked(i + 1))
                            ImGui.EndDisabled();
                    }

                    ImGui.SameLine();
                }


                if (ImGui.Button("Deselect"))
                    Workshops.Keys.ToList().ForEach(key => Workshops[key] = false);
            }
            catch (Exception ex)
            {
                ex.Log();
            }
            if (autoWorkshopSelect)
                ImGui.EndDisabled();

            try
            {
                var ScheduleContainsRest = MultiCycleList.Any(x => x.PrimarySchedule.Any(y => y.OnRestDay == true));
                if (ScheduleContainsRest)
                {
                    ImGui.TextColored(ImGuiColors.TankBlue, $"{(overrideRest ? "Blue cycle's rest will be overriden" : "Blue cycle will be set to rest")}");
                    ImGui.Checkbox("Override Rest?", ref overrideRest);
                }

                var BadFortune = GetCurrentRestDays()[1] != 1 && MultiCycleList.Count == 5;
                if (BadFortune)
                {
                    ImGui.TextColored(ImGuiColors.DalamudRed, "Detected Fortuneteller import.\nYour rest cycle isn't set to rest on cycle 2.");
                    ImGui.Checkbox("Schedule anyway?", ref overrideExecutionDisable);
                    ImGuiComponents.HelpMarker("Overseas Casuals always recommends resting on C1 and C2 when doing fortune teller recommendations.\n" +
                        "Execution has been disabled as a sanity check, but if this import is on purpose, feel free to check the override box.");
                }

                var IsInsufficientRank = (PrimarySchedule.Count > 0 && PrimarySchedule.Any(x => x.InsufficientRank))
                    || (SecondarySchedule.Count > 0 && SecondarySchedule.Any(x => x.InsufficientRank));
                var ScheduleInProgress = selectedCycle - 1 <= MJIManager.Instance()->CurrentCycleDay && selectedCycle != 0;
                var restDays = GetCurrentRestDays();
                var SelectedIsRest = restDays.Contains(selectedCycle - 1);
                var NoWorkshopsSelected = Workshops.Values.All(x => !x) && !autoWorkshopSelect;

                if (IsInsufficientRank || ScheduleInProgress || SelectedIsRest || NoWorkshopsSelected || (BadFortune && !overrideExecutionDisable))
                {
                    if (IsInsufficientRank)
                        ImGui.TextColored(ImGuiColors.DalamudRed, "Insufficient rank to execute schedule");
                    if (SelectedIsRest)
                        ImGui.TextColored(ImGuiColors.DalamudRed, "Selected cycle is a rest day.\nCannot schedule on a rest day.");
                    if (ScheduleInProgress)
                        ImGui.TextColored(ImGuiColors.DalamudRed, "Cannot execute schedule on days\nin progress or passed");
                    if (NoWorkshopsSelected)
                        ImGui.TextColored(ImGuiColors.DalamudRed, "No workshops selected.\nTurn on Auto-Select or select workshops.");

                    ImGui.BeginDisabled();
                }

                if (ImGui.Button("Execute Schedule"))
                {
                    currentWorkshop = Workshops.FirstOrDefault(pair => pair.Value).Key;
                    currentDay = (selectedCycle == 0 ? MJIManager.Instance()->CurrentCycleDay + nextDayOffset
                        : selectedCycle);
                    ScheduleMultiCycleList();
                }

                if (IsInsufficientRank || ScheduleInProgress || SelectedIsRest || NoWorkshopsSelected || (BadFortune && !overrideExecutionDisable))
                    ImGui.EndDisabled();
            }
            catch (Exception e) { PluginLog.Log(e.ToString()); return; }
        }

        private bool IsWorkshopUnlocked(int w)
        {
            try
            {
                var currentRank = MJIManager.Instance()->IslandState.CurrentRank;
                
                var workshopRanks = new Dictionary<int, int>
                {
                    { 1, 3 },
                    { 2, 6 },
                    { 3, 8 },
                    { 4, 14 }
                };

                if (workshopRanks.TryGetValue(w, out var requiredRank))
                {
                    maxWorkshops = w - 1;
                    return currentRank >= requiredRank;
                }

                return false;
            }
            catch (Exception ex)
            {
                ex.Log();
                return false;
            }
        }

        private static void DrawWorkshopListBox(string text, List<Item> schedule)
        {
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
                            if (item.OnRestDay && !item.InsufficientRank)
                                ImGui.TextColored(ImGuiColors.TankBlue, item.Name);
                            else if (item.InsufficientRank && !item.OnRestDay)
                                ImGui.TextColored(ImGuiColors.DalamudRed, item.Name);
                            else if (item.OnRestDay && item.InsufficientRank)
                                ImGuiEx.Text(GradientColor.Get(ImGuiColors.DalamudRed, ImGuiColors.TankBlue), item.Name);
                        }
                        else
                            ImGui.Text(item.Name);
                    }
                }
                ImGui.EndListBox();
            }
        }

        internal static void ScheduleImport(List<string> rawItemStrings)
        {
            var rawCycles = SplitCycles(rawItemStrings);

            foreach (var cycle in rawCycles)
            {
                (var items, var excessItems) = ParseItems(cycle);
                if (items == null || items.Count == 0)
                    continue;
                MultiCycleList.Add(new CyclePreset { PrimarySchedule = items, SecondarySchedule = excessItems });
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
                        cycles.Add(currentCycle);
                        currentCycle = new List<string>();
                    }
                    if (currentCycle.Count == 0)
                        currentCycle = new List<string>();
                }
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
                    PluginLog.Debug($"Failed to match string to craftable: {itemString}");
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

        private static unsafe bool OpenCycle(int cycle_day)
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

        private bool WaitForAddButton(int workshopIndex)
        {
            uint id = workshopIndex switch
            {
                0 => 8,
                1 => 80001,
                2 => 80002,
                3 => 80003,
                _ => 0
            };
            return TryGetAddonByName<AtkUnitBase>("MJICraftSchedule", out var addon) && id != 0 && addon->GetNodeById(id)->IsVisible;
        }

        private static unsafe bool OpenAgenda(int workshop, int prevHours)
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


                Callback.Fire(workshopWindow, false, 16, (uint)(workshop), (uint)(prevHours));

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

        private bool SetRestDay(int cycle)
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
                if (cycle <= 7 && cycle > 0)
                    restDays[1] = cycle - 1;
                else if (cycle > 7)
                    restDays[3] = cycle - 1;
                else if (cycle <= 0)
                    restDays[1] = MJIManager.Instance()->CurrentCycleDay + 1;

                var restDaysMask = restDays.Sum(n => (int)Math.Pow(2, n));
                Callback.Fire(schedulerWindow, false, 11, (uint)restDaysMask);

                PluginLog.Debug($"Setting Rest Days to {string.Join("", restDays)} => {restDaysMask}");
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
            if (isScheduleRest)
            {
                //var currentVal = selectedCycle;
                //TaskManager.EnqueueImmediate(() => selectedCycle = currentDay, $"SetSelectedCycleToCurrentDay");
                //TaskManager.EnqueueImmediate(() => SetRestDay(currentVal), $"SetRest");
                //TaskManager.EnqueueImmediate(() => selectedCycle = currentVal, $"SetSelectedCycleBackToOriginal");
                return true;
            }

            var hours = 0;
            if (autoWorkshopSelect)
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
                            TaskManager.DelayNextImmediate("PSOpenAgendaDelay", PrimarySchedule.IndexOf(item) == 0 ? Config.taskAfterCycleSwitchDelay : Config.taskDelay);
                            TaskManager.EnqueueImmediate(() => WaitForAddButton(ws));
                            TaskManager.EnqueueImmediate(() => OpenAgenda(ws, hours), $"PSOpenAgendaW{ws + 1}");
                            TaskManager.DelayNextImmediate("PSScheduleItemDelay", Config.taskDelay);
                            TaskManager.EnqueueImmediate(() => ScheduleItem(item), $"PSScheduleItem:{item.Name}:W{ws + 1}");
                            TaskManager.EnqueueImmediate(() => hours += item.CraftingTime, $"PSIncrementHoursW{ws + 1}");
                        }
                    }
                    TaskManager.EnqueueImmediate(() => hours = 0, $"SSSetHours0");
                    foreach (var item in SecondarySchedule)
                    {
                        TaskManager.DelayNextImmediate("SSOpenAgendaDelay", SecondarySchedule.IndexOf(item) == 0 ? Config.taskAfterCycleSwitchDelay : Config.taskDelay);
                        TaskManager.EnqueueImmediate(() => WaitForAddButton(3));
                        TaskManager.EnqueueImmediate(() => OpenAgenda(3, hours), $"SSOpenAgendaW{maxWorkshops}");
                        TaskManager.DelayNextImmediate("SSScheduleItemDelay", Config.taskDelay);
                        TaskManager.EnqueueImmediate(() => ScheduleItem(item), $"SSSchedule:{item.Name}:W{maxWorkshops}");
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
                            TaskManager.DelayNextImmediate("PSOOpenAgendaDelay", PrimarySchedule.IndexOf(item) == 0 ? Config.taskAfterCycleSwitchDelay : Config.taskDelay);
                            TaskManager.EnqueueImmediate(() => WaitForAddButton(ws));
                            TaskManager.EnqueueImmediate(() => OpenAgenda(ws, hours), $"PSOOpenAgendaW{ws + 1}");
                            TaskManager.DelayNextImmediate("PSOScheduleItemDelay", Config.taskDelay);
                            TaskManager.EnqueueImmediate(() => ScheduleItem(item), $"PSOSchedule:{item.Name}:W{ws + 1}");
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
                            TaskManager.EnqueueImmediate(() => PluginLog.Log($"{item.Name} : {item.UIIndex} : {hours}"));
                            TaskManager.DelayNextImmediate("MOpenAgendaDelay", PrimarySchedule.IndexOf(item) == 0 ? Config.taskAfterCycleSwitchDelay : Config.taskDelay);
                            TaskManager.EnqueueImmediate(() => WaitForAddButton(ws));
                            TaskManager.EnqueueImmediate(() => OpenAgenda(ws, hours), $"MOpenAgendaW{ws + 1}");
                            TaskManager.DelayNextImmediate("MScheduleItemDelay", Config.taskDelay);
                            TaskManager.EnqueueImmediate(() => ScheduleItem(item), $"MSchedule:{item.Name}:W{ws + 1}");
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
            var restCycleIndex = MultiCycleList.FindIndex(x => x.PrimarySchedule.Any(y => y.OnRestDay == true));
            if (restCycleIndex != -1 && !overrideRest)
            {
                // delay when cycle that is open is the one being set to rest?
                TaskManager.Enqueue(() => SetRestDay(currentDay + restCycleIndex), $"MultiCycleSetRestOn{currentDay + restCycleIndex}");
            }

            TaskManager.Enqueue(() =>
            {
                foreach (var cycle in MultiCycleList)
                {
                    if (MultiCycleList.IndexOf(cycle) > 0 && !autoWorkshopSelect)
                        return;

                    TaskManager.Enqueue(() => OpenCycle(currentDay), $"MultiCycleOpenCycleOn{currentDay}");
                    TaskManager.Enqueue(() => PrimarySchedule = cycle.PrimarySchedule, $"MultiCycleSetPrimaryCycleOn{currentDay}");
                    TaskManager.Enqueue(() => SecondarySchedule = cycle.SecondarySchedule, $"MultiCyleSetSecondaryCycleOn{currentDay}");
                    TaskManager.Enqueue(() => { isScheduleRest = overrideRest ? false : PrimarySchedule[0].OnRestDay; }, $"MultiCycleCheckRestOn{currentDay}");
                    TaskManager.Enqueue(() => ScheduleList(), $"MultiCycleScheduleListOn{currentDay}");
                    TaskManager.Enqueue(() => currentDay += 1, $"MultiCycleScheduleIncrementDayFrom{currentDay}");
                }
            }, "ScheduleMultiCycleForEach");
        }

        private void CheckIfInvalidSchedule(ref SeString message, ref bool isHandled)
        {
            // Unable to set agenda. Insufficient time for handicraft production.
            if (message.ExtractText() == Svc.Data.GetExcelSheet<LogMessage>().First(x => x.RowId == 10146).Text.ExtractText())
            {
                PluginLog.Log("Detected error in scheduling. Aborting current workshop's queue.");
                TaskManager.Abort();
                if (WorkshopsRemaining())
                {
                    TaskManager.Enqueue(() => currentWorkshop += 1);
                    ScheduleList();
                }
            }
        }

        private bool WorkshopsRemaining() => Workshops.Skip(currentWorkshop).Any(pair => pair.Value);

        private void OnWorkshopSetup(SetupAddonArgs obj)
        {
            if (obj.AddonName != "MJICraftSchedule") return;

            if (Config.OpenNextDay)
                OpenCycle(MJIManager.Instance()->CurrentCycleDay + 2);

            if (Config.AutoImport)
            {
                try
                {
                    MultiCycleList.Clear();

                    var text = ImGui.GetClipboardText();
                    if (text.IsNullOrEmpty()) return;

                    var rawItemStrings = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    ScheduleImport(rawItemStrings);

                    if (MultiCycleList.All(x => x.PrimarySchedule.Count == 0))
                        PrintPluginMessage("Failed to parse any items from clipboard.");
                }
                catch (Exception e)
                {
                    PluginLog.Error($"Could not parse clipboard. Clipboard may be empty.\n{e}");
                }
            }
        }

        private void AutoSell(SetupAddonArgs obj)
        {
            if (obj.AddonName != "MJIDisposeShop") return;
            if (!Config.AutoSell) return;

            Callback.Fire(obj.Addon, false, 13, Config.AutoSellAmount);
            AutoSellConfirm();
        }

        private void AutoSellConfirm()
        {
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("MJIDisposeShopShippingBulk");
            TaskManager.Enqueue(() => addon != null && addon->IsVisible);
            TaskManager.Enqueue(() => Callback.Fire(addon, true, 0));
        }

        private void AutoCollectPasture(SetupAddonArgs obj)
        {
            if (obj.AddonName != "MJIAnimalManagement") return;
            if (!Config.AutoCollectPasture) return;
            if (obj.Addon->AtkValues[219].Byte == 0) return;

            Callback.Fire(obj.Addon, false, 5);
            AutoYesNo();
        }

        private void AutoCollectFarm(SetupAddonArgs obj)
        {
            if (obj.AddonName != "MJIFarmManagement") return;
            if (!Config.AutoCollectFarm) return;
            if (obj.Addon->AtkValues[195].Byte == 0) return;

            Callback.Fire(obj.Addon, false, 3);
            AutoYesNo();
        }

        private void AutoCollectGranary(SetupAddonArgs obj)
        {
            if (obj.AddonName != "MJIGatheringHouse") return;
            if (!Config.AutoCollectGranary) return;

            if (obj.Addon->AtkValues[73].Int != 0)
                TaskManager.Enqueue(() => Callback.Fire(obj.Addon, false, 13, 0));
            if (obj.Addon->AtkValues[147].Int != 0)
                TaskManager.Enqueue(() => Callback.Fire(obj.Addon, false, 13, 1));
        }

        //private void AutoSetGranary(SetupAddonArgs obj)
        //{
        //    if (obj.AddonName != "MJIGatheringHouse") return;
        //    var agent = new AgentMJIPouch();
        //    var x = agent.GetInventory()->Span.ToString();
        //    PluginLog.Log($"{x}");

        //    if (Config.AutoConfirmGranary)
        //        Callback.Fire(obj.Addon, false, 15, 0, 0);
        //}

        private void AutoMaxGranary(SetupAddonArgs obj)
        {
            if (obj.AddonName != "MJISearchArea") return;
            if (!Config.AutoMaxGranary) return;

            if (!obj.Addon->UldManager.NodeList[8]->GetAsAtkTextNode()->NodeText.ToString().Equals("7/7"))
                Callback.Fire(obj.Addon, false, 14, 7, 0);
        }

        private void AutoYesNo()
        {
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("SelectYesno");
            TaskManager.Enqueue(() => addon != null && addon->IsVisible && addon->UldManager.NodeList[15]->IsVisible);
            TaskManager.Enqueue(() => new ClickSelectYesNo((IntPtr)addon).Yes());
        }

        //private void AutoConfirmGranary(SetupAddonArgs obj)
        //{
        //    if (obj.AddonName != "SelectYesno") return;
        //    if (!Config.AutoConfirmGranary) return;

        //    if (TryGetAddonByName<AddonSelectYesno>("SelectYesno", out var addon) &&
        //            addon->AtkUnitBase.IsVisible &&
        //            addon->YesButton->IsEnabled &&
        //            addon->AtkUnitBase.UldManager.NodeList[15]->IsVisible)
        //        new ClickSelectYesNo((IntPtr)obj.Addon).Yes();
        //}

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
            Svc.Toasts.ErrorToast += CheckIfInvalidSchedule;
            Common.OnAddonSetup += OnWorkshopSetup;
            Common.OnAddonSetup += AutoSell;
            Common.OnAddonSetup += AutoCollectPasture;
            Common.OnAddonSetup += AutoCollectFarm;
            Common.OnAddonSetup += AutoCollectGranary;
            //Common.OnAddonSetup += AutoSetGranary;
            Common.OnAddonSetup += AutoMaxGranary;
            base.Enable();
        }

        public override void Disable()
        {
            SaveConfig(Config);
            P.Ws.RemoveWindow(overlay);
            Svc.Toasts.ErrorToast -= CheckIfInvalidSchedule;
            Common.OnAddonSetup -= OnWorkshopSetup;
            Common.OnAddonSetup -= AutoSell;
            Common.OnAddonSetup -= AutoCollectPasture;
            Common.OnAddonSetup -= AutoCollectFarm;
            Common.OnAddonSetup -= AutoCollectGranary;
            //Common.OnAddonSetup -= AutoSetGranary;
            Common.OnAddonSetup -= AutoMaxGranary;
            base.Disable();
        }
    }
}
