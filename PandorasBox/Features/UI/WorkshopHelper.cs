using ClickLib.Clicks;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Logging;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using static ECommons.GenericHelpers;

// TODO:
// add config to auto select next cycle when opening workshop addon
// prevent schedule from executing if workshop has anything filled in
// add option to auto remove invalid entries
// display other workshops in listbox if autoguess is on

namespace PandorasBox.Features.UI
{
    public unsafe class WorkshopHelper : Feature
    {
        public override string Name => "Workshop Helper";

        public override string Description => "Adds a menu to the Island Sanctuary workshop to allow quick setting your daily schedules. Supports importing from Overseas Casuals.";

        public override FeatureType FeatureType => FeatureType.UI;
        private Overlays Overlay;
        public static bool ResetPosition = false;
        public static bool _enabled;

        internal static (uint Key, string Name, ushort CraftingTime, ushort LevelReq)[] Craftables;
        public static List<Item> PrimarySchedule;
        public static List<Item> SecondarySchedule;

        private Dictionary<int, bool> Workshops = new Dictionary<int, bool> { [0] = false, [1] = false, [2] = false, [3] = false };
        private int CurrentWorkshop;
        private int MAX_WORKSHOPS = 4;
        private List<int> Cycles { get; set; } = new() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };
        private int SelectedCycle = 0;
        private bool IsScheduleRest;
        private bool AutoGuess;
        private bool Fortuneteller;

        public class SchedulePreset
        {
            public List<Item> Items { get; }
        }

        public class Item
        {
            public uint Key { get; set; }
            public string Name { get; set; }
            public ushort CraftingTime { get; set; }
            public uint UIIndex { get; set; }
            public ushort LevelReq { get; set; }
            public bool InsufficientRank {  get; set; }
        }

        public override void Draw()
        {
            if (_enabled)
            {
                var workshopWindow = Svc.GameGui.GetAddonByName("MJICraftSchedule", 1);
                if (workshopWindow == IntPtr.Zero)
                    return;

                var addonPtr = (AtkUnitBase*)workshopWindow;
                if (addonPtr == null)
                    return;

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
                    var text = ImGui.GetClipboardText();
                    if (text.IsNullOrEmpty()) return;
                    List<string> rawItemStrings = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    ScheduleImport(rawItemStrings);
                }
                catch (Exception e)
                {
                    PluginLog.Error($"Could not parse clipboard. Clipboard may be empty.\n{e}");
                }
            }
            ImGuiComponents.HelpMarker("This importer detects the presence of an item's name (not including \"Isleworks\") on each line.\nYou can copy the entire day's schedule from the discord, junk included. If anything is not matched properly, it will show as an invalid entry and you will need to reimport.");
            // ImGui.Checkbox("Fortuneteller Import", ref Fortuneteller);
            // ImGuiComponents.HelpMarker("ENGLISH ONLY. OVERSEAS CASUALS DISCORD ONLY. This follows the standard format the Casuals use where Cycle 1 & 2 are always rest.\nThis will read the items listed between \"Cycle\" lines and assume they are for Cycles 3 onwards.");
            if (!Fortuneteller)
            {
                ImGui.Checkbox("Guess Workshops", ref AutoGuess);
                ImGuiComponents.HelpMarker("If the cumulative hours of your clipboard is >24, this will assume the first 24 hours\nare for workshops 1-3 and the remaining are for workshop 4. If it is 24 or less, it will apply to all four workshops.");
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
            if (AutoGuess && SecondarySchedule == null)
            {
                DrawWorkshopListBox("Workshops 1-4", PrimarySchedule);
            }
            else if (AutoGuess && SecondarySchedule != null)
            {
                DrawWorkshopListBox("Workshops 1-3", PrimarySchedule);
                DrawWorkshopListBox("Workshop 4", SecondarySchedule);
            }
            else
                DrawWorkshopListBox("", PrimarySchedule);


            if (!Fortuneteller)
            {
                ImGui.Text("Select Cycle");
                ImGuiComponents.HelpMarker("Leave blank to execute on open cycle.");
                ImGui.SetNextItemWidth(100);
                var cyclePrev = SelectedCycle == 0 ? "" : Cycles[SelectedCycle - 1].ToString();
                if (ImGui.BeginCombo("", cyclePrev))
                {
                    if (ImGui.Selectable("", SelectedCycle == 0))
                        SelectedCycle = 0;
                    foreach (var cycle in Cycles)
                    {
                        var selected = ImGui.Selectable(cycle.ToString(), SelectedCycle == cycle);

                        if (selected)
                            SelectedCycle = cycle;
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
                    TaskManager.Enqueue(() => OpenCycle(SelectedCycle - 1));
                    TaskManager.Enqueue(() => SelectedCycle -= 1);
                }
                ImGui.SameLine();
                if (ImGui.Button("Next"))
                {
                    TaskManager.Enqueue(() => OpenCycle(SelectedCycle + 1));
                    TaskManager.Enqueue(() => SelectedCycle += 1);
                }

                if (AutoGuess)
                    ImGui.BeginDisabled();
                ImGui.Text("Select Workshops");
                var currentRank = MJIManager.Instance()->IslandState.CurrentRank;
                for (var i = 0; i < Workshops.Count; i++)
                {
                    var configValue = Workshops[i];
                    if (ImGui.Checkbox($"{i + 1}", ref configValue)) { Workshops[i] = configValue; }
                    if (i != Workshops.Count - 1) ImGui.SameLine();
                }
                ImGui.SameLine();
                if (ImGui.Button("Deselect"))
                    Workshops.Keys.ToList().ForEach(key => Workshops[key] = false);
                if (AutoGuess)
                    ImGui.EndDisabled();
            }

            try
            {
                var IsInsufficientRank = (PrimarySchedule != null && PrimarySchedule.Any(x => x.InsufficientRank))
                    || (SecondarySchedule != null && SecondarySchedule.Any(x => x.InsufficientRank));
                if (IsInsufficientRank)
                {
                    ImGui.BeginDisabled();
                    ImGui.TextColored(ImGuiColors.DalamudRed, "Insufficient rank to execute schedule");
                }
                var restDays = GetCurrentRestDays();
                if (restDays.Contains(SelectedCycle - 1))
                {
                    ImGui.BeginDisabled();
                    ImGui.TextColored(ImGuiColors.DalamudRed, "Selected cycle is a rest day. Cannot schedule on a rest day.");
                }
                if (ImGui.Button("Execute Schedule"))
                {
                    CurrentWorkshop = Workshops.FirstOrDefault(pair => pair.Value).Key;
                    if (Fortuneteller)
                        ScheduleFortuneteller();
                    else
                        ScheduleList();
                }
                if (IsInsufficientRank || restDays.Contains(SelectedCycle - 1))
                    ImGui.EndDisabled();
            }
            catch (Exception e) { PluginLog.Log(e.ToString()); return; }
        }

        private static void DrawWorkshopListBox(string text, List<Item> schedule)
        {
            if (!text.IsNullOrEmpty())
                ImGui.Text(text);
            ImGui.SetNextItemWidth(250);
            if (ImGui.BeginListBox($"##Listbox{text}", new Vector2(250, 6 * ImGui.GetTextLineHeightWithSpacing())))
            {
                if (schedule != null)
                {
                    foreach (var item in schedule)
                    {
                        if (item.InsufficientRank)
                            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);

                        ImGui.Text(item.Name);

                        if (item.InsufficientRank)
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
            if (!Fortuneteller)
            {
                (List<Item> items, List<Item> excessItems) = ParseItems(rawItemStrings);
                PrimarySchedule = items;
                SecondarySchedule = excessItems;
            }
            else
            {
                List<List<string>> fortuneCycles = new List<List<string>>();
                List<string> currentCycle = new List<string>();
                string pattern = @"Cycle\s+\d+\s+(.*?)(?=Cycle|$)";
                MatchCollection matches = Regex.Matches(string.Join(Environment.NewLine, rawItemStrings), pattern, RegexOptions.Singleline);
                foreach (Match match in matches)
                {
                    var extract = match.Groups[1].Value.Trim();
                    if (extract.StartsWith("Cycle"))
                        if (currentCycle.Count > 0)
                        {
                            fortuneCycles.Add(currentCycle);
                            currentCycle = new List<string>();
                        }
                    else
                        currentCycle.Add(extract);
                }

                if (currentCycle.Count > 0)
                    fortuneCycles.Add(currentCycle);

                foreach (var cycle in fortuneCycles)
                {
                    (List<Item> items, List<Item> excessItems) = ParseItems(cycle);
                    PrimarySchedule = items;
                    SecondarySchedule = excessItems;
                }
            }
        }

        public static (List<Item>, List<Item>) ParseItems(List<string> itemStrings)
        {
            List<Item> items = new List<Item>();
            List<Item> excessItems = new List<Item>();
            var hours = 0;
            foreach (var itemString in itemStrings)
            {
                var matchFound = false;
                foreach (var craftable in Craftables)
                {
                    if (IsMatch(itemString.ToLower(), craftable.Name.ToLower()))
                    {
                        Item item = new Item
                        {
                            Key = craftable.Key,
                            Name = Svc.Data.GetExcelSheet<MJICraftworksObject>().GetRow(craftable.Key).Item.Value.Name.RawString,
                            CraftingTime = craftable.CraftingTime,
                            UIIndex = craftable.Key - 1,
                            LevelReq = craftable.LevelReq
                        };
                        item.InsufficientRank = isCraftworkObjectCraftable(item);
                        PluginLog.Log($"{item.InsufficientRank}");

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
                    Item invalidItem = new Item
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
            string pattern = $@"\b{Regex.Escape(y)}\b";
            return Regex.IsMatch(x, pattern);
        }

        private static bool isCraftworkObjectCraftable(Item item) => !(MJIManager.Instance()->IslandState.CurrentRank < item.LevelReq);

        private bool isWorkshopOpen() => Svc.GameGui.GetAddonByName("MJICraftSchedule") != IntPtr.Zero;

        private unsafe bool OpenCycle(int cycle_day)
        {
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("MJICraftSchedule");
            if (!isWorkshopOpen() || !GenericHelpers.IsAddonReady(addon)) return false;
            TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Selecting Cycle", 300));
            TaskManager.EnqueueImmediate(() => EzThrottler.Check("Selecting Cycle"));

            try
            {
                var workshopPTR = Svc.GameGui.GetAddonByName("MJICraftSchedule");
                if (workshopPTR == IntPtr.Zero)
                    return false;

                var workshopWindow = (AtkUnitBase*)workshopPTR;
                if (workshopWindow == null)
                    return false;

                Callback.Fire(workshopWindow, false, 19, (uint)(cycle_day - 1));

                // var SelectCycle = stackalloc AtkValue[2];
                // SelectCycle[0] = new()
                // {
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                //     Int = 19,
                // };
                // SelectCycle[1] = new()
                // {
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                //     UInt = (uint)(cycle_day - 1),
                // };
                // workshopWindow->FireCallback(1, SelectCycle);

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
            TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Opening Agenda", 300));
            TaskManager.EnqueueImmediate(() => EzThrottler.Check("Opening Agenda"));

            try
            {
                var workshopPTR = Svc.GameGui.GetAddonByName("MJICraftSchedule");
                if (workshopPTR == IntPtr.Zero)
                    return false;

                var workshopWindow = (AtkUnitBase*)workshopPTR;
                if (workshopWindow == null)
                    return false;


                Callback.Fire(workshopWindow, false, 16, (uint)(workshop), (uint)(index == 0 ? 0 : prevHours));

                // var SelectAgenda = stackalloc AtkValue[3];
                // SelectAgenda[0] = new()
                // {
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                //     Int = 16,
                // };
                // SelectAgenda[1] = new()
                // {
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                //     UInt = (uint)(workshop),
                // };
                // SelectAgenda[1] = new()
                // {
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                //     UInt = (uint)(index == 0 ? 0 : prevHours),
                // };
                // workshopWindow->FireCallback(1, SelectAgenda);

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

                // var SelectItem = stackalloc AtkValue[2];
                // SelectItem[0] = new()
                // {
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                //     Int = 11,
                // };
                // SelectItem[1] = new()
                // {
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                //     UInt = key.id,
                // };
                // schedulerWindow->FireCallback(1, SelectItem);
                // TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Selecting Item", 300));
                // TaskManager.EnqueueImmediate(() => EzThrottler.Check("Selecting Item"));

                // var Schedule = stackalloc AtkValue[1];
                // Schedule[0] = new()
                // {
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                //     Int = 13,
                // };
                // schedulerWindow->FireCallback(1, Schedule);
                // schedulerWindow->Close(true);
                // TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Schedule Button", 300));
                // TaskManager.EnqueueImmediate(() => EzThrottler.Check("Schedule Button"));

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
                TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Setting Rest Days", 300));
                TaskManager.EnqueueImmediate(() => EzThrottler.Check("Setting Rest Days"));

                var restDaysPTR = Svc.GameGui.GetAddonByName("MJICraftScheduleMaintenance");
                if (restDaysPTR == IntPtr.Zero)
                    return false;
                var schedulerWindow = (AtkUnitBase*)restDaysPTR;
                if (schedulerWindow == null)
                    return false;

                var restDays = GetCurrentRestDays();
                if (SelectedCycle <= 6)
                    restDays[1] = SelectedCycle - 1;
                else if (SelectedCycle >= 7)
                    restDays[3] = SelectedCycle - 1;
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

        public void ScheduleList()
        {
            if (SelectedCycle != 0)
            {
                TaskManager.Enqueue(() => OpenCycle(SelectedCycle));
            }

            if (IsScheduleRest)
            {
                TaskManager.Enqueue(() => SetRestDay());
                return;
            }

            var hours = 0;
            if (AutoGuess)
            {
                if (SecondarySchedule != null)
                {
                    for (var i = 0; i < MAX_WORKSHOPS - 1; i++)
                    {
                        var ws = 0;
                        TaskManager.Enqueue(() => hours = 0);
                        foreach (Item item in PrimarySchedule)
                        {
                            ws = i;
                            TaskManager.Enqueue(() => OpenAgenda(item.UIIndex, ws, hours));
                            TaskManager.Enqueue(() => ScheduleItem(item));
                            TaskManager.Enqueue(() => hours += item.CraftingTime);
                        }
                    }
                    TaskManager.Enqueue(() => hours = 0);
                    foreach (Item item in SecondarySchedule)
                    {
                        TaskManager.Enqueue(() => OpenAgenda(item.UIIndex, 3, hours));
                        TaskManager.Enqueue(() => ScheduleItem(item));
                        TaskManager.Enqueue(() => hours += item.CraftingTime);
                    }
                }
                else
                {
                    for (var i = 0; i < MAX_WORKSHOPS; i++)
                    {
                        var ws = 0;
                        TaskManager.Enqueue(() => hours = 0);
                        foreach (Item item in PrimarySchedule)
                        {
                            ws = i;
                            TaskManager.Enqueue(() => OpenAgenda(item.UIIndex, ws, hours));
                            TaskManager.Enqueue(() => ScheduleItem(item));
                            TaskManager.Enqueue(() => hours += item.CraftingTime);
                        }
                    }
                }
            }
            else
            {
                for (var i = CurrentWorkshop; i < Workshops.Count; i++)
                {
                    var ws = 0;
                    if (Workshops[i])
                    {
                        TaskManager.Enqueue(() => hours = 0);
                        foreach (Item item in PrimarySchedule)
                        {
                            ws = i;
                            TaskManager.Enqueue(() => OpenAgenda(item.UIIndex, ws, hours));
                            TaskManager.Enqueue(() => ScheduleItem(item));
                            TaskManager.Enqueue(() => hours += item.CraftingTime);
                            TaskManager.Enqueue(() => CurrentWorkshop += 1);
                        }
                    }
                }
                TaskManager.Enqueue(() => CurrentWorkshop = 0);
            }
        }

        public void ScheduleFortuneteller()
        {
            TaskManager.Enqueue(() => OpenCycle(2));
            TaskManager.Enqueue(() => SetRestDay());
        }

        private void CheckIfInvalidSchedule(ref SeString message, ref bool isHandled)
        {
            if (message.ExtractText() == Svc.Data.GetExcelSheet<LogMessage>().First(x => x.RowId == 10146).Text.ExtractText())
            {
                TaskManager.Abort();
                if (WorkshopsRemaining())
                {
                    TaskManager.Enqueue(() => CurrentWorkshop += 1);
                    ScheduleList();
                }
            }
        }

        private bool WorkshopsRemaining()
        {
            return Workshops.Skip(CurrentWorkshop).Any(pair => pair.Value);
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
            Craftables = Svc.Data.GetExcelSheet<MJICraftworksObject>(Dalamud.ClientLanguage.English)
                .Where(x => x.Item.Row > 0)
                .Select(x => (x.RowId, x.Item.Value.Name.RawString.Replace("Isleworks", "").Replace("Islefish", "").Replace("Isleberry", "").Trim(), x.CraftingTime, x.LevelReq))
                .ToArray();
            Overlay = new Overlays(this);
            _enabled = true;
            Svc.Toasts.ErrorToast += CheckIfInvalidSchedule;
            base.Enable();
        }

        public override void Disable()
        {
            P.Ws.RemoveWindow(Overlay);
            _enabled = false;
            Svc.Toasts.ErrorToast -= CheckIfInvalidSchedule;
            base.Disable();
        }
    }
}
