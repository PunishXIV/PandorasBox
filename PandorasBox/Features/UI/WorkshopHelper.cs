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
            public uint UIIndex { get; set; }
        }

        // internal static Dictionary<int, Schedule> ScheduleImport(string input)
        // {
        //     List<Item> items = ParseItems(itemStrings);

        //     Dictionary<int, Schedule> schedules = new Dictionary<int, Schedule>();
        //     foreach (int workshop in workshops)
        //     {
        //         schedules[workshop] = new Schedule(items);
        //     }

        //     CopiedSchedule = schedules;
        //     return schedules;
        // }

        public static List<Item> ParseItems(List<string> itemStrings)
        {
            List<Item> items = new List<Item>();
            foreach (var itemString in itemStrings)
            {
                foreach (var craftable in Craftables)
                {
                    string craftableNoPrefix = craftable.Name.Replace("Isleworks ", "");
                    if (itemString.Contains(craftableNoPrefix))
                    {
                        Item item = new Item
                        {
                            Key = craftable.Key,
                            Name = craftable.Name,
                            CraftingTime = craftable.CraftingTime,
                            UIIndex = craftable.Key - 1
                        };
                        // PluginLog.Log($"matched {itemString} to {craftable.Name}");

                        items.Add(item);
                    }
                    // else { PluginLog.Log($"failed to match {itemString} to {craftableNoPrefix}"); }
                }
            }

            return items;
        }

        public void TestSchedule()
        {
            PluginLog.Log($"entering test schedule");
            List<string> itemStrings = new List<string> { "Firesand", "Garnet Rapier", "Earrings", "Silver Ear Cuffs" };

            List<Item> items = ParseItems(itemStrings);
            List<int> workshops = new List<int> { 1 };
            int hours = 0;
            foreach (Item item in items)
            {
                PluginLog.Log($"queueing agenda open");
                TaskManager.Enqueue(() => OpenAgenda(item.UIIndex, workshops[0], hours));
                PluginLog.Log($"queueing schedule");
                TaskManager.Enqueue(() => ScheduleItem(item, workshops[0]));
            }
            return;
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

        private unsafe bool ScheduleItem(Item item, int workshop)
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

                Callback.Fire(schedulerWindow, false, 11, item.Key);
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

        protected override DrawConfigDelegate DrawConfigTree => (ref bool _) =>
    {
        var x1 = new Item { Key = Craftables[0].Key, Name = Craftables[0].Name, CraftingTime = Craftables[0].CraftingTime, UIIndex = Craftables[0].Key - 1 };
        var x2 = new Item { Key = Craftables[21].Key, Name = Craftables[21].Name, CraftingTime = Craftables[21].CraftingTime, UIIndex = Craftables[0].Key - 1 };
        var x3 = new Item { Key = Craftables[16].Key, Name = Craftables[16].Name, CraftingTime = Craftables[16].CraftingTime, UIIndex = Craftables[0].Key - 1 };
        var x4 = new Item { Key = Craftables[24].Key, Name = Craftables[24].Name, CraftingTime = Craftables[24].CraftingTime, UIIndex = Craftables[0].Key - 1 };
        if (ImGui.Button("Debug Craftables"))
        {
            List<Item> items = new List<Item>();
            foreach (var x in Craftables)
                // items.Add(new Item(x.Key, x.Name, x.CraftingTime, x.Key-1));
                PluginLog.Log($"K: {x.Key}, N: {x.Name}, CT: {x.CraftingTime}, UI: {x.Key - 1}");
        }
        if (ImGui.Button("Debug List"))
        {
            PluginLog.Log($"K: {Craftables[0].Key}, N: {Craftables[0].Name}, CT: {Craftables[0].CraftingTime}, UI: {Craftables[0].Key - 1}");
            PluginLog.Log($"K: {x1.Key}, N: {x1.Name}, CT: {x1.CraftingTime}, UI: {x1.UIIndex}");
            // PluginLog.Log($"K: {x2.Key}, N: {x2.Name}, CT: {x2.CraftingTime}, UI: {x2.UIIndex}");
            // PluginLog.Log($"K: {x3.Key}, N: {x3.Name}, CT: {x3.CraftingTime}, UI: {x3.UIIndex}");
            // PluginLog.Log($"K: {x4.Key}, N: {x4.Name}, CT: {x4.CraftingTime}, UI: {x4.UIIndex}");
            // PluginLog.Log($"K: {x1.Key}, K-1= UI: {x1.UIIndex}");
            // PluginLog.Log($"K: {x2.Key}, K-1= UI: {x2.UIIndex}");
            // PluginLog.Log($"K: {x3.Key}, K-1= UI: {x3.UIIndex}");
            // PluginLog.Log($"K: {x4.Key}, K-1= UI: {x4.UIIndex}");
        }
        if (ImGui.Button("Schedule"))
        {
            TaskManager.Enqueue(() => OpenAgenda(x1.UIIndex, 1, 0));
            TaskManager.Enqueue(() => ScheduleItem(x1, 1));
            TaskManager.Enqueue(() => OpenAgenda(x2.UIIndex, 1, x1.CraftingTime));
            TaskManager.Enqueue(() => ScheduleItem(x2, 1));
            TaskManager.Enqueue(() => OpenAgenda(x3.UIIndex, 1, x2.CraftingTime));
            TaskManager.Enqueue(() => ScheduleItem(x3, 1));
            TaskManager.Enqueue(() => OpenAgenda(x4.UIIndex, 1, x3.CraftingTime));
            TaskManager.Enqueue(() => ScheduleItem(x4, 1));
        }
    };

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Craftables = Svc.Data.GetExcelSheet<MJICraftworksObject>()
                .Where(x => x.Item.Row > 0)
                .Select(x => (x.RowId, x.Item.Value.Name.RawString, x.CraftingTime))
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
