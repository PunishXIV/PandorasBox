using ClickLib.Clicks;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Interface;
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

        private Dictionary<int, bool> Workshops = new Dictionary<int, bool> { [0] = false, [1] = false, [2] = false, [3] = false };
        private int CurrentWorkshop;
        private List<int> Cycles { get; set; } = new() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14 };
        private bool IsScheduleRest;

        public Configs Config { get; private set; }
        public class Configs : FeatureConfig
        {
            public int SelectedCycle = 0;
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
                    CopiedSchedule = ScheduleImport(rawItemStrings);
                }
                catch (Exception e)
                {
                    PluginLog.Error($"Could not parse clipboard. Clipboard may be empty.\n{e}");
                }
            }
            ImGuiComponents.HelpMarker("This importer detects the presence of an item's name (not including \"Isleworks\") on each line.\nYou can copy the entire day's schedule from the discord, junk included. If anything is not matched properly, it will show as an invalid entry and you will need to reimport.");

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
            ImGui.SetNextItemWidth(250);
            if (ImGui.BeginListBox("##Listbox", new Vector2(250, 6 * ImGui.GetTextLineHeightWithSpacing())))
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

            ImGui.Text("Select Cycle");
            ImGuiComponents.HelpMarker("Leave blank to execute on open cycle.");
            ImGui.SetNextItemWidth(100);
            var cyclePrev = Config.SelectedCycle == 0 ? "" : Cycles[Config.SelectedCycle - 1].ToString();
            if (ImGui.BeginCombo("", cyclePrev))
            {
                if (ImGui.Selectable("", Config.SelectedCycle == 0))
                    Config.SelectedCycle = 0;
                foreach (var cycle in Cycles)
                {
                    var selected = ImGui.Selectable(cycle.ToString(), Config.SelectedCycle == cycle);

                    if (selected)
                        Config.SelectedCycle = cycle;
                }
                ImGui.EndCombo();
            }
            ImGui.SameLine();
            if (ImGui.Button("Set as Rest Day"))
            {
                TaskManager.Enqueue(() => SetRestDay());
            }

            ImGui.Text("Select Workshops");
            for (var i = 0; i < Workshops.Count; i++)
            {
                var configValue = Workshops[i];
                if (ImGui.Checkbox($"{i + 1}", ref configValue)) { Workshops[i] = configValue; }
                if (i != Workshops.Count - 1) ImGui.SameLine();
            }

            try
            {
                if (ImGui.Button("Execute Schedule"))
                {
                    CurrentWorkshop = Workshops.FirstOrDefault(pair => pair.Value).Key;
                    var restDays = GetCurrentRestDays();
                    if (restDays.Contains(Config.SelectedCycle - 1))
                        PrintPluginMessage("Selected cycle is a rest day. Cannot schedule on a rest day.");
                    else
                        ScheduleList();
                }
            }
            catch (Exception e) { PluginLog.Log(e.ToString()); return; }
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

                        items.Add(item);
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
                restDays[1] = Config.SelectedCycle - 1;
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
            if (Config.SelectedCycle != 0)
            {
                TaskManager.Enqueue(() => OpenCycle(Config.SelectedCycle));
            }

            // add rest handling here when that's implemented
            if (IsScheduleRest)
            {
                TaskManager.Enqueue(() => SetRestDay());
            }

            int hours = 0;
            for (var i = CurrentWorkshop; i < Workshops.Count; i++)
            {
                var ws = 0;
                if (Workshops[i])
                {
                    TaskManager.Enqueue(() => hours = 0);
                    foreach (Item item in CopiedSchedule)
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

        public static void PrintPluginMessage(String msg)
        {
            var message = new XivChatEntry
            {
                Message = new SeStringBuilder()
                .AddUiForeground($"[Pandora's Box] ", 45)
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
                .Select(x => (x.RowId, x.Item.Value.Name.RawString, x.CraftingTime))
                .ToArray();
            Overlay = new Overlays(this);
            _enabled = true;
            Svc.Toasts.ErrorToast += CheckIfInvalidSchedule;
            base.Enable();
        }

        public override void Disable()
        {
            SaveConfig(Config);
            P.Ws.RemoveWindow(Overlay);
            _enabled = false;
            Svc.Toasts.ErrorToast -= CheckIfInvalidSchedule;
            base.Disable();
        }
    }
}
