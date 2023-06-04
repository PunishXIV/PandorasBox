using Dalamud.Interface.Windowing;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Logging;
using ECommons.Throttlers;
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

namespace PandorasBox.Features.UI
{
    public unsafe class WorkshopHelper : Feature
    {
        public override string Name => "Workshop Helper";

        public override string Description => "Save/Load infinite presets. Set the schedule(s) for you. Can import from Overseas Casuals.";

        public override FeatureType FeatureType => FeatureType.UI;
        internal WorkshopWindow WorkshopWindow { get; set; }
        internal static (uint Key, string Name, ushort CraftingTime)[] Craftables;
        public static Dictionary<int, Schedule> CopiedSchedule;
        public static bool _enabled;

        public class Configs : FeatureConfig
        {
            public int SelectedCycle = 1;
        }

        public Configs Config { get; private set; }

        public class Schedule
        {
            public List<Item> Items { get; }

            public Schedule(List<Item> items)
            {
                Items = items;
            }
        }

        public class Item
        {
            public uint Key { get; set; }
            public string Name { get; set; }
            public ushort CraftingTime { get; set; }
            public byte UIIndex { get; set; }
        }

        internal static Dictionary<int, Schedule> ScheduleImport(string input)
        {
            List<int> workshops = ParseWorkshops(input);
            List<string> itemStrings = ParseItems(input);
            List<Item> items = MatchItems(itemStrings);

            Dictionary<int, Schedule> schedules = new Dictionary<int, Schedule>();
            foreach (int workshop in workshops)
            {
                schedules[workshop] = new Schedule(items);
            }

            CopiedSchedule = schedules;
            return schedules;
        }

        public static List<int> ParseWorkshops(string input)
        {
            List<int> workshops = new List<int>();

            string pattern = @"Workshops #(\d+)-?(\d+)? Rec|All Workshops|All";
            Match match = Regex.Match(input, pattern);

            if (match.Success)
            {
                if (match.Groups[1].Success) // Single workshop
                {
                    int workshopNumber = int.Parse(match.Groups[1].Value);
                    workshops.Add(workshopNumber);
                }
                else if (match.Groups[2].Success) // Range of workshops
                {
                    int start = int.Parse(match.Groups[1].Value);
                    int end = int.Parse(match.Groups[2].Value);

                    for (int i = start; i <= end; i++)
                    {
                        workshops.Add(i);
                    }
                }
                else if (match.Value.Equals("All Workshops", StringComparison.OrdinalIgnoreCase) ||
                         match.Value.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    // Add all workshops (1, 2, 3, 4) to the list
                    for (int i = 1; i <= 4; i++)
                    {
                        workshops.Add(i);
                    }
                }
            }

            return workshops;
        }

        internal static List<string> ParseItems(string input)
        {
            List<string> itemStrings = new List<string>();

            string pattern = @"(?<=: )(.*?)(?= \()";
            MatchCollection matches = Regex.Matches(input, pattern);
            foreach (Match match in matches)
            {
                string itemString = match.Groups[1].Value;
                itemStrings.Add(itemString);
            }

            return itemStrings;
        }


        public static List<Item> MatchItems(List<string> itemStrings)
        {
            List<Item> items = new List<Item>();
            foreach (string itemName in itemStrings)
            {
                var matchedCraftable = Craftables.FirstOrDefault(c => c.Name.Contains(itemName, StringComparison.OrdinalIgnoreCase));

                if (matchedCraftable.Key != 0)
                {
                    Item item = new Item
                    {
                        Key = matchedCraftable.Key,
                        Name = matchedCraftable.Name,
                        CraftingTime = matchedCraftable.CraftingTime,
                        UIIndex = (byte)matchedCraftable.Key
                    };

                    items.Add(item);
                }
                else
                {
                    PluginLog.Log($"Item not found: {itemName}");
                }
            }

            return items;
        }

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

        private unsafe bool OpenAgenda(int index, int workshop, int prevHours)
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


                Callback.Fire(workshopWindow, false, 16, (uint)(workshop - 1), (uint)(index == 0 ? 0 : prevHours));

                // var SelectAgenda = stackalloc AtkValue[3];
                // SelectAgenda[0] = new()
                // {
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                //     Int = 16,
                // };
                // SelectAgenda[1] = new()
                // {
                //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                //     UInt = (uint)(workshop - 1),
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

        // private unsafe bool ScheduleItem(ItemValues key, int workshop)
        // {
        //     var addon = Svc.GameGui.GetAddonByName("MJICraftSchedule");
        //     if (addon == IntPtr.Zero)
        //         return false;
        //     if (!GenericHelpers.IsAddonReady((AtkUnitBase*)addon)) return false;

        //     try
        //     {
        //         var schedulerPTR = Svc.GameGui.GetAddonByName("MJICraftScheduleSetting");
        //         if (schedulerPTR == IntPtr.Zero)
        //             return false;
        //         var schedulerWindow = (AtkUnitBase*)schedulerPTR;
        //         if (schedulerWindow == null)
        //             return false;

        //         Callback.Fire(schedulerWindow, false, 11, key.id);
        //         Callback.Fire(schedulerWindow, false, 13);
        //         schedulerWindow->Close(true);

        //         // var SelectItem = stackalloc AtkValue[2];
        //         // SelectItem[0] = new()
        //         // {
        //         //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
        //         //     Int = 11,
        //         // };
        //         // SelectItem[1] = new()
        //         // {
        //         //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
        //         //     UInt = key.id,
        //         // };
        //         // schedulerWindow->FireCallback(1, SelectItem);
        //         // TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Selecting Item", 300));
        //         // TaskManager.EnqueueImmediate(() => EzThrottler.Check("Selecting Item"));

        //         // var Schedule = stackalloc AtkValue[1];
        //         // Schedule[0] = new()
        //         // {
        //         //     Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
        //         //     Int = 13,
        //         // };
        //         // schedulerWindow->FireCallback(1, Schedule);
        //         // schedulerWindow->Close(true);
        //         // TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Schedule Button", 300));
        //         // TaskManager.EnqueueImmediate(() => EzThrottler.Check("Schedule Button"));

        //         return true;
        //     }
        //     catch
        //     {
        //         return false;
        //     }
        // }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Craftables = Svc.Data.GetExcelSheet<MJICraftworksObject>()
                .Where(x => !string.IsNullOrEmpty(x.Item.ToString()) || !string.IsNullOrEmpty(x.Theme.ToString()))
                .Select(x => (x.RowId, x.Item.Value.ToString(), x.CraftingTime))
                .ToArray();
            WorkshopWindow = new();
            P.Ws.AddWindow(WorkshopWindow);
            _enabled = true;
            base.Enable();
        }

        public override void Disable()
        {
            SaveConfig(Config);
            P.Ws.RemoveWindow(WorkshopWindow);
            WorkshopWindow = null;
            _enabled = false;
            base.Disable();
        }
    }
}
