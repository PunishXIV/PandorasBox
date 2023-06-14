using Dalamud.Interface;
using Dalamud.Interface.Components;
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
        private Overlays Overlay;
        public static bool ResetPosition = false;
        public static bool _enabled;

        internal static (uint Key, string Name, ushort CraftingTime)[] Craftables;
        public static List<Item> CopiedSchedule;

        // private bool[] Workshops = new bool[4] { false, false, false, false };
        private Dictionary<int, bool> Workshops = new Dictionary<int, bool> { [0] = false, [1] = false, [2] = false, [3] = false };
        private List<string> Cycles { get; set; } = new() { "", "C1", "C2", "C3", "C4", "C5", "C6", "C7" };

        public Configs Config { get; private set; }
        public class Configs : FeatureConfig
        {
            public int SelectedCycle = 1;
        }


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
                        ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(400f, 400f));
                        ImGui.Begin($"###Options{node->NodeID}", ImGuiWindowFlags.NoScrollbar
                            | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.AlwaysUseWindowPadding);

                        DrawWindowContents();

                        ImGui.End();
                        ImGui.PopStyleVar(2);
                    }
                }
            }
        }
        //     protected override DrawConfigDelegate DrawConfigTree => (ref bool _) =>
        // {
        //     if (ImGui.Button("Debug")) { DebugMethod(); }
        // };

        private void DrawWindowContents()
        {
            ImGui.Columns(2, "SchedulerOptionsColumns", true);

            if (ImGui.Button("Overseas Casuals Import"))
            {
                try
                {
                    var text = ImGui.GetClipboardText();
                    if (text.IsNullOrEmpty()) return;
                    List<string> rawItemStrings = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();
                    CopiedSchedule = ScheduleImport(rawItemStrings);
                }
                catch (Exception e)
                {
                    PluginLog.Error($"Could not parse clipboard. Clipboard may be empty.\n{e}");
                }
            }
            ImGuiComponents.HelpMarker("This importer detects the presence an item's name (not including \"Isleworks\") on each line.\nYou can copy the entire day's schedule from the discord, junk included. If anything is not matched properly, it will show as an invalid entry and you can manually edit it.");
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

            if (ImGui.BeginListBox("##Listbox", new Vector2(ImGui.GetColumnWidth(), 100)))
            {
                if (CopiedSchedule != null)
                {
                    foreach (Item item in CopiedSchedule)
                    {
                        if (ImGui.Selectable(item.Name)) return;
                    }
                }
                ImGui.EndListBox();
            }

            try { if (ImGui.Button("Execute Schedule")) { ScheduleList(); } }
            catch (Exception e) { PluginLog.Log(e.ToString()); return; }

            try { if (ImGui.Button("debug config")) { DebugMethod(); } }
            catch (Exception e) { PluginLog.Log(e.ToString()); return; }

            ImGui.NextColumn();

            if (ImGui.BeginCombo("Cycles", Cycles[0]))
            {
                for (int i = 0; i < Cycles.Count; i++)
                {
                    bool isSelected = (Cycles[i] == Cycles[0]);
                    if (ImGui.Selectable(Cycles[i], isSelected))
                        Cycles[0] = Cycles[i];

                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }
            ImGuiComponents.HelpMarker("Leave blank to execute the schedule on whichever cycle is currently loaded in the in-game menu.");
            for (var i = 0; i < Workshops.Count; i++)
            {
                var configValue = Workshops[i];
                if (ImGui.Checkbox($"W{i + 1}", ref configValue)) { Workshops[i] = configValue; }
            }

            ImGui.Columns(1);
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
                bool matchFound = false;
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
                        matchFound = true;
                    }
                }
                if (!matchFound)
                {
                    PluginLog.Log($"invalid item {itemString}");
                    Item invalidItem = new Item
                    {
                        Key = 0,
                        Name = "Invalid",
                        CraftingTime = 0,
                        UIIndex = 0
                    };
                    items.Add(invalidItem);
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

        private unsafe bool OpenAgenda(uint index, int workshop, int prevHours)
        {
            PluginLog.Log($"openagenda {workshop}");
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

        public void ScheduleList()
        {
            int hours = 0;
            for (var i = 0; i < Workshops.Count; i++)
            {
                // PluginLog.Log($"i outside loop: {i}");
                if (Workshops[i])
                {
                    TaskManager.Enqueue(() => hours = 0);
                    foreach (Item item in CopiedSchedule)
                    {
                        PluginLog.Log($"i before pass: {i}");
                        TaskManager.Enqueue(() => OpenAgenda(item.UIIndex, i, hours));
                        TaskManager.Enqueue(() => ScheduleItem(item));
                        TaskManager.Enqueue(() => hours += item.CraftingTime);
                    }
                }
            }
        }

        public void DebugMethod()
        {
            for (var i = 0; i < Workshops.Count; i++) PluginLog.Log(Workshops[i].ToString());
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Craftables = Svc.Data.GetExcelSheet<MJICraftworksObject>()
                .Where(x => x.Item.Row > 0)
                .Select(x => (x.RowId, x.Item.Value.Name.RawString, x.CraftingTime))
                .ToArray();
            Overlay = new Overlays(this);
            _enabled = true;
            base.Enable();
        }

        public override void Disable()
        {
            SaveConfig(Config);
            P.Ws.RemoveWindow(Overlay);
            _enabled = false;
            base.Disable();
        }
    }
}
