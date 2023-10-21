using ClickLib.Clicks;
using Dalamud;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
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

        public Configs Config { get; private set; }
        public override bool UseAutoConfig => true;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Automatically go to the next day's cycle when opening the workshop menu.", "", 1)]
            public bool OpenNextDay = false;

            [FeatureConfigOption("Automatically import from clipboard when loading the workshop.", "", 2)]
            public bool AutoImport = false;

            [FeatureConfigOption("Automatically export materials when speaking with the export mammet.", "", 4)]
            public bool AutoSell = false;
            public bool ShouldShowAutoSellAmount() => AutoSell;

            [FeatureConfigOption("Auto Sell Above", "", 5, IntMin = 0, IntMax = 999, EditorSize = 300, ConditionalDisplay = true)]
            public int AutoSellAmount = 900;

            [FeatureConfigOption("Automatically collect drops from pasture", "", 6)]
            public bool AutoCollectPasture = false;

            [FeatureConfigOption("Automatically collect crops from farm", "", 7)]
            public bool AutoCollectFarm = false;

            [FeatureConfigOption("Auto Collect Granary", "", 8)]
            public bool AutoCollectGranary = false;

            [FeatureConfigOption("Auto Max Granary", "", 11)]
            public bool AutoMaxGranary = false;
        }

        private Overlays overlay;
        public static bool ResetPosition;

        private static SetupAddonArgs CachedAddonSetup;

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

        private readonly List<string> prefixes = new() { "Isleworks", "Islefish", "Isleberry", "Island" };

        public class CyclePreset
        {
            public int Cycle { get; set; }
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

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Craftables = Svc.Data.GetExcelSheet<MJICraftworksObject>()
                .Where(x => x.Item.Row > 0)
                .Select(x =>
                {
                    var itemName = x.Item.GetDifferentLanguage(ClientLanguage.English).Value.Name.RawString;
                    itemName = prefixes.Aggregate(itemName, (current, prefix) => current.Replace(prefix, "")).Trim();

                    return (x.RowId, itemName, x.CraftingTime, x.LevelReq);
                })
                .ToArray();
            overlay = new Overlays(this);

            Svc.Toasts.ErrorToast += CheckIfInvalidSchedule;

            Common.OnAddonSetup += OnWorkshopSetup;
            Common.OnAddonSetup += AutoSell;
            Common.OnAddonSetup += AutoCollectPasture;
            Common.OnAddonSetup += AutoCollectFarm;
            Svc.Framework.Update += AutoCollectGranary;
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
            Svc.Framework.Update -= AutoCollectGranary;
            Common.OnAddonSetup -= AutoMaxGranary;

            base.Disable();
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
                        PrintModuleMessage("Failed to parse any items from clipboard. Refer to help icon for how to import.");

                    OpenCycle(MultiCycleList.First().Cycle - 1);
                    selectedCycle = MultiCycleList.First().Cycle - 1;
                }
                catch (Exception e)
                {
                    PrintModuleMessage("Failed to parse any items from clipboard. Refer to help icon for how to import.");
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

            foreach (var cycle in MultiCycleList)
            {
                if (MultiCycleList.IndexOf(cycle) > 0 && !autoWorkshopSelect)
                    continue;

                var cycleNum = MultiCycleList.IndexOf(cycle) + 1 + selectedCycle;

                DrawWorkshopListBox($"Cycle {cycleNum} Workshops {(!autoWorkshopSelect ? string.Join(", ", Workshops.Where(x => x.Value).Select(x => x.Key + 1)) : (cycle.SecondarySchedule.Count > 0 ? "1-3" : "1-4"))}", cycle.PrimarySchedule);

                if (cycle.SecondarySchedule.Count > 0 && autoWorkshopSelect)
                    DrawWorkshopListBox($"Cycle {cycleNum} Workshop 4", cycle.SecondarySchedule);
            }
            ImGui.EndChild();

            ImGui.Text("Select Cycle");

            ImGui.SetNextItemWidth(100);
            var cyclePrev = Cycles[selectedCycle].ToString();
            if (ImGui.BeginCombo("", cyclePrev))
            {
                foreach (var cycle in Cycles)
                {
                    var selected = ImGui.Selectable(cycle.ToString(), selectedCycle == cycle - 1);

                    if (selected)
                    {
                        selectedCycle = cycle - 1;
                        OpenCycle(selectedCycle);
                    }

                }
                ImGui.EndCombo();
            }
            ImGui.SameLine();

            var SelectedUnavailable = selectedCycle <= MJIManager.Instance()->CurrentCycleDay || GetCurrentRestDays().Contains(selectedCycle);
            if (SelectedUnavailable)
                ImGui.BeginDisabled();

            if (ImGui.Button("Set Rest"))
            {
                TaskManager.Enqueue(() => SetRestDay(selectedCycle));
            }

            if (SelectedUnavailable)
                ImGui.EndDisabled();

            var restDays = GetCurrentRestDays();


            ImGui.SameLine();
            if (ImGui.Button("Prev"))
            {
                if (!restDays.Contains(selectedCycle - 1))
                    TaskManager.Enqueue(() => OpenCycle(selectedCycle));
                selectedCycle = selectedCycle == 0 ? 0 : selectedCycle - 1;
            }

            ImGui.SameLine();
            if (ImGui.Button("Next"))
            {
                if (!restDays.Contains(selectedCycle + 1))
                    TaskManager.Enqueue(() => OpenCycle(selectedCycle));
                selectedCycle = selectedCycle == 13 ? 13 : selectedCycle + 1;
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
                        if (!IslandSanctuaryHelper.IsWorkshopUnlocked(i + 1, out var _))
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

                        if (!IslandSanctuaryHelper.IsWorkshopUnlocked(i + 1, out var _))
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
                var ScheduleInProgress = selectedCycle < MJIManager.Instance()->CurrentCycleDay;
                var NoWorkshopsSelected = Workshops.Values.All(x => !x) && !autoWorkshopSelect;
                var SelectedIsRest = restDays.Contains(selectedCycle);

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
                    currentDay = (int)(selectedCycle == 0 ? IslandSanctuaryHelper.GetOpenCycle() : selectedCycle);
                    ScheduleMultiCycleList();
                }

                if (IsInsufficientRank || ScheduleInProgress || SelectedIsRest || NoWorkshopsSelected || (BadFortune && !overrideExecutionDisable))
                    ImGui.EndDisabled();
            }
            catch (Exception e) { PluginLog.Log(e.ToString()); return; }
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
            var cycles = ParseCycles(rawItemStrings);

            foreach (var cycle in cycles)
            {
                (var items, var excessItems) = ParseItems(rawItemStrings, cycle);
                if (items == null || items.Count == 0)
                    continue;
                MultiCycleList.Add(new CyclePreset { Cycle = cycle, PrimarySchedule = items, SecondarySchedule = excessItems });
            }
        }

        public static List<int> ParseCycles(List<string> rawLines)
        {
            var output = new List<int>();

            foreach (var line in rawLines)
            {
                if (line.StartsWith("Cycle"))
                {
                    var cycleStr = Regex.Match(line, @"\d+").Value;
                    var cycleNo = int.Parse(cycleStr);
                    output.Add(cycleNo);
                }
            }

            return output;
        }

        public static (List<Item>, List<Item>) ParseItems(List<string> itemStrings, int cycle)
        {
            var items = new List<Item>();
            var excessItems = new List<Item>();
            var hours = 0;
            var isRest = false;

            var indexOfCycleStart = itemStrings.IndexOf(x => x.StartsWith("Cycle") && int.Parse(Regex.Match(x, @"\d+").Value) == cycle);
            var indexOfNextCycleStart = itemStrings.IndexOf(x => x.StartsWith("Cycle") && int.Parse(Regex.Match(x, @"\d+").Value) == cycle + 1);
            if (indexOfNextCycleStart == -1)
                indexOfNextCycleStart = itemStrings.Count;

            if (indexOfCycleStart == -1)
                indexOfCycleStart = 0;

            if (indexOfCycleStart > 0 && itemStrings.IndexOf(x => x.StartsWith("Cycle") && int.Parse(Regex.Match(x, @"\d+").Value) == cycle - 1) == -1)
            {
                (var firstItems, var firstExcess) = ParseItems(itemStrings, cycle - 1);
                MultiCycleList.Add(new CyclePreset { Cycle = cycle == 1 ? 14 : cycle - 1, PrimarySchedule = firstItems, SecondarySchedule = firstExcess });
            }

            for (var i = indexOfCycleStart + 1; i < indexOfNextCycleStart; i++)
            {
                var itemString = itemStrings[i];

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
                        item.InsufficientRank = !IslandSanctuaryHelper.isCraftworkObjectCraftable(Svc.Data.GetExcelSheet<MJICraftworksObject>().GetRow(craftable.Key));

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
                }
            }

            return (items, excessItems);
        }

        private static bool IsMatch(string x, string y) => Regex.IsMatch(x, $@"\b{Regex.Escape(y)}\b");

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

        private static unsafe bool OpenCycle(int cycle_day)
        {
            if (TryGetAddonByName<AtkUnitBase>("MJICraftSchedule", out var addon))
            {
                Callback.Fire(addon, false, 20, (uint)(cycle_day));
                if (addon->AtkValues[0].Type == 0) return false;
                return true;
                //return addon->AtkValues[0].UInt == cycle_day;
            }
            else
                return false;
        }

        private static unsafe bool OpenAgenda(int workshop, int prevHours)
        {
            if (TryGetAddonByName<AtkUnitBase>("MJICraftSchedule", out var addon))
            {
                Callback.Fire(addon, false, 17, (uint)(workshop), (uint)(prevHours));
                return IslandSanctuaryHelper.isCraftSelectOpen();
            }
            else
                return false;
        }

        private unsafe bool ScheduleItem(Item item)
        {
            if (TryGetAddonByName<AtkUnitBase>("MJICraftScheduleSetting", out var addon))
            {
                Callback.Fire(addon, false, 11, item.UIIndex);
                Callback.Fire(addon, false, 13);
                addon->Close(true);
                return !IslandSanctuaryHelper.isCraftSelectOpen();
            }
            else
                return false;
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
                    restDays[1] = cycle;
                else if (cycle > 7)
                    restDays[3] = cycle;
                else if (cycle <= 0)
                    restDays[1] = MJIManager.Instance()->CurrentCycleDay;

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
            if (isScheduleRest) return true;

            var hours = 0;
            var logPrefix = autoWorkshopSelect ? $"{nameof(PrimarySchedule)}Only" : "Manual";

            if (autoWorkshopSelect)
            {
                for (var i = 0; i < maxWorkshops; i++)
                {
                    var ws = 0;
                    var schedule = PrimarySchedule;

                    if (SecondarySchedule.Count > 0 && i < maxWorkshops - 1)
                    {
                        schedule = PrimarySchedule;
                        logPrefix = $"{nameof(PrimarySchedule)}";
                    }
                    else if (SecondarySchedule.Count > 0)
                    {
                        schedule = SecondarySchedule;
                        logPrefix = $"{nameof(SecondarySchedule)}";
                    }

                    TaskManager.EnqueueImmediate(() => hours = 0, $"{logPrefix}.SetHours0");

                    foreach (var item in schedule)
                    {
                        ws = i;
                        TaskManager.EnqueueImmediate(() => WaitForAddButton(ws), 200, $"{logPrefix}.{nameof(WaitForAddButton)}.{ws}");
                        TaskManager.EnqueueImmediate(() => OpenAgenda(ws, hours), $"{logPrefix}.{nameof(OpenAgenda)}.{ws}");
                        TaskManager.EnqueueImmediate(() => ScheduleItem(item), $"{logPrefix}.{nameof(ScheduleItem)}.{item.Name}");
                        TaskManager.EnqueueImmediate(() => hours += item.CraftingTime, $"{logPrefix}.Increment{nameof(hours)}W.{ws + 1}");
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
                        TaskManager.EnqueueImmediate(() => hours = 0, $"{logPrefix}.Set{nameof(hours)}0");
                        foreach (var item in PrimarySchedule)
                        {
                            ws = i;
                            TaskManager.EnqueueImmediate(() => PluginLog.Log($"{item.Name} : {item.UIIndex} : {hours}"));
                            TaskManager.EnqueueImmediate(() => WaitForAddButton(ws), 200, $"{logPrefix}.{nameof(WaitForAddButton)}.{ws}");
                            TaskManager.EnqueueImmediate(() => OpenAgenda(ws, hours), $"{logPrefix}.{nameof(OpenAgenda)}.{ws}");
                            TaskManager.EnqueueImmediate(() => ScheduleItem(item), $"{logPrefix}.{nameof(ScheduleItem)}.{item.Name}");
                            TaskManager.EnqueueImmediate(() => hours += item.CraftingTime, $"{logPrefix}.Increment{nameof(hours)}W.{ws + 1}");
                            TaskManager.EnqueueImmediate(() => currentWorkshop += 1, $"{logPrefix}.Increment{nameof(currentWorkshop)}FromW.{ws + 1}");
                        }
                    }
                }
                TaskManager.EnqueueImmediate(() => currentWorkshop = 0, $"{logPrefix}.Set{nameof(currentWorkshop)}0");
            }
            return true;
        }

        public void ScheduleMultiCycleList()
        {
            var restCycleIndex = MultiCycleList.FindIndex(x => x.PrimarySchedule.Any(y => y.OnRestDay == true));
            if (restCycleIndex != -1 && !overrideRest)
            {
                // delay when cycle that is open is the one being set to rest?
                TaskManager.Enqueue(() => SetRestDay(currentDay + restCycleIndex), $"{nameof(ScheduleMultiCycleList)}.{nameof(SetRestDay)}{currentDay + restCycleIndex}");
            }

            TaskManager.Enqueue(() =>
            {
                foreach (var cycle in MultiCycleList)
                {
                    if (MultiCycleList.IndexOf(cycle) > 0 && !autoWorkshopSelect)
                        return;

                    TaskManager.Enqueue(() => OpenCycle(currentDay), $"{nameof(ScheduleMultiCycleList)}.OpenCycleOn{currentDay}");
                    TaskManager.Enqueue(() => PrimarySchedule = cycle.PrimarySchedule, $"{nameof(ScheduleMultiCycleList)}.Set{nameof(PrimarySchedule)}On{currentDay}");
                    TaskManager.Enqueue(() => SecondarySchedule = cycle.SecondarySchedule, $"{nameof(ScheduleMultiCycleList)}.Set{nameof(SecondarySchedule)}On{currentDay}");
                    TaskManager.Enqueue(() => { isScheduleRest = !overrideRest && PrimarySchedule[0].OnRestDay; }, $"{nameof(ScheduleMultiCycleList)}.CheckRestOn{currentDay}");
                    TaskManager.Enqueue(() => ScheduleList(), $"{nameof(ScheduleMultiCycleList)}.{nameof(ScheduleList)}On{currentDay}");
                    TaskManager.Enqueue(() => currentDay += 1, $"{nameof(ScheduleMultiCycleList)}.ScheduleIncrementDayFrom{currentDay}");
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
            CachedAddonSetup = obj;
            TaskManager.Enqueue(() => CachedAddonSetup.Addon->AtkValues[0].Type != 0, $"WaitingFor{CachedAddonSetup.AddonName}");
            TaskManager.Enqueue(() =>
            {
                if (Config.OpenNextDay)
                {
                    OpenCycle(MJIManager.Instance()->CurrentCycleDay + 1);
                    selectedCycle = MJIManager.Instance()->CurrentCycleDay + 1;
                }

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
                            PrintModuleMessage("Failed to parse any items from clipboard.");

                        OpenCycle(MultiCycleList.First().Cycle - 1);
                        selectedCycle = MultiCycleList.First().Cycle - 1;

                    }
                    catch (Exception e)
                    {
                        PluginLog.Error($"Could not parse clipboard. Clipboard may be empty.\n{e}");
                    }
                }
            }, $"{nameof(OnWorkshopSetup)}");
        }

        private void AutoSell(SetupAddonArgs obj)
        {
            if (obj.AddonName != "MJIDisposeShop" || obj.Addon is null) return;
            if (!Config.AutoSell) return;

            Callback.Fire(obj.Addon, false, 13, Config.AutoSellAmount);
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("MJIDisposeShopShippingBulk");
            if (addon != null)
                Callback.Fire(addon, true, 0);
        }

        private void AutoCollectPasture(SetupAddonArgs obj)
        {
            if (obj.AddonName != "MJIAnimalManagement") return;
            if (!Config.AutoCollectPasture) return;
            if (obj.Addon->AtkValues[219].Byte != 0) return;

            Callback.Fire(obj.Addon, false, 5);
            AutoYesNo();
        }

        private void AutoCollectFarm(SetupAddonArgs obj)
        {
            if (obj.AddonName != "MJIFarmManagement") return;
            if (!Config.AutoCollectFarm) return;
            if (obj.Addon->AtkValues[195].Byte != 0) return;

            Callback.Fire(obj.Addon, false, 3);
            AutoYesNo();
        }

        private void AutoCollectGranary(IFramework framework)
        {
            if (!Config.AutoCollectGranary) return;

            if (TryGetAddonByName<AtkUnitBase>("MJIGatheringHouse", out var addon))
            {
                if (!TaskManager.IsBusy)
                {
                    if (addon->AtkValues[73].Byte != 0)
                    {
                        TaskManager.Enqueue(() => { TryGetAddonByName<AtkUnitBase>("MJIGatheringHouse", out var granary1); Callback.Fire(granary1, false, 13, 0); }, "CollectGranary1");
                        TaskManager.DelayNext(200);
                        TaskManager.Enqueue(() => AutoYesNo());
                    }
                    if (addon->AtkValues[147].Byte != 0)
                    {
                        TaskManager.Enqueue(() => { TryGetAddonByName<AtkUnitBase>("MJIGatheringHouse", out var granary2); Callback.Fire(granary2, false, 13, 1); }, "CollectGranary2");
                        TaskManager.DelayNext(200);
                        TaskManager.Enqueue(() => AutoYesNo());
                    }
                }
            }
        }

        private unsafe void AutoMaxGranary(SetupAddonArgs obj)
        {
            if (obj.AddonName != "MJISearchArea") return;
            if (!Config.AutoMaxGranary) return;

            obj.Addon->GetButtonNodeById(45)->ClickAddonButton((AtkComponentBase*)obj.Addon, 26);
        }

        private void AutoYesNo()
        {
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("SelectYesno");
            if (addon != null && addon->IsVisible && addon->UldManager.NodeList[15]->IsVisible)
                new ClickSelectYesNo((IntPtr)addon).Yes();
        }
    }
}
