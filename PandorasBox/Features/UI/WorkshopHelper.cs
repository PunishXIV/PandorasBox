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
        public static List<Item> CopiedSchedule;
        public static bool _enabled;

        public class Configs : FeatureConfig
        {
            public List<bool> Workshops = new List<bool> { true, true, true, true };
            public int SelectedCycle = 1;
        }

        public Configs Config { get; private set; }

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
        }

        internal static List<Item> ScheduleImport(List<string> rawItemStrings)
        {
            List<Item> items = ParseItems(rawItemStrings);
            CopiedSchedule = items;
            return CopiedSchedule;
        }

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
                TaskManager.Enqueue(() => OpenAgenda(item.UIIndex, workshops[0], hours));
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

        public void ScheduleList()
        {
            int hours = 0;
            foreach (var ws in Config.Workshops)
            {
                if (ws)
                {
                    TaskManager.Enqueue(() => hours = 0);
                    foreach (Item item in CopiedSchedule)
                    {
                        TaskManager.Enqueue(() => OpenAgenda(item.UIIndex, Config.Workshops.IndexOf(ws) + 1, hours));
                        TaskManager.Enqueue(() => ScheduleItem(item, Config.Workshops.IndexOf(ws) + 1));
                        TaskManager.Enqueue(() => hours += item.CraftingTime);
                    }
                }
            }
        }

        // protected override DrawConfigDelegate DrawConfigTree => (ref bool _) =>
        // {
        //     if (ImGui.Button("Import from clipboard")) { }
        //     if (ImGui.Checkbox("W1", ref Config.w1)) { Config.Workshops.Add(1); }
        //     ImGui.SameLine();
        //     if (ImGui.Checkbox("W2", ref Config.w2)) { Config.Workshops.Add(2); }
        //     ImGui.SameLine();
        //     if (ImGui.Checkbox("W3", ref Config.w3)) { Config.Workshops.Add(3); }
        //     ImGui.SameLine();
        //     if (ImGui.Checkbox("W4", ref Config.w4)) { Config.Workshops.Add(4); }
        //     if (ImGui.Button("Fire Schedule"))
        //     {
        //         List<string> itemStrings = new List<string> { "Firesand", "Garnet Rapier", "Earrings", "Silver Ear Cuffs" };

        //         List<Item> items = ParseItems(itemStrings);
        //         List<int> workshops = new List<int> { 1, 3 };
        //         int hours = 0;
        //         foreach (var ws in workshops)
        //         {
        //             TaskManager.Enqueue(() => hours = 0);
        //             foreach (Item item in items)
        //             {
        //                 TaskManager.Enqueue(() => OpenAgenda(item.UIIndex, ws, hours));
        //                 TaskManager.Enqueue(() => ScheduleItem(item, ws));
        //                 TaskManager.Enqueue(() => hours += item.CraftingTime);
        //             }
        //         }
        //         List<string> is2 = new List<string> { "Powdered Paprika", "Vegetable Juice", "Powdered Paprika", "Vegetable Juice", "Powdered Paprika" };
        //         List<Item> i2 = ParseItems(is2);
        //         TaskManager.Enqueue(() => hours = 0);
        //         foreach (Item item in i2)
        //         {
        //             TaskManager.Enqueue(() => OpenAgenda(item.UIIndex, 4, hours));
        //             TaskManager.Enqueue(() => ScheduleItem(item, 4));
        //             TaskManager.Enqueue(() => hours += item.CraftingTime);
        //         }
        //     }
        // };

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
