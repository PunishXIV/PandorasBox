using ClickLib.Clicks;
using Dalamud.Interface;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using PandorasBox.UI;
using PandorasBox.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static ECommons.GenericHelpers;

namespace PandorasBox.Features.UI
{
    public unsafe class Desynth : Feature
    {
        public override string Name => "Desynth All";

        public override string Description => "Adds a button to the desynthesis window to desynth all from the current dropdown. (Disclaimer: Pandora takes no responsibility for the loss of any Ultimate weapons or other rare items. Please use responsibly.)";

        private delegate IntPtr UpdateItemDelegate(IntPtr a1, ulong index, IntPtr a3, ulong a4);
        private delegate byte UpdateListDelegate(IntPtr a1, IntPtr a2, IntPtr a3);

        private HookWrapper<UpdateItemDelegate> updateItemHook;
        private HookWrapper<UpdateListDelegate> updateListHook;

        private delegate void* SetupDropDownList(AtkComponentList* a1, ushort a2, byte** a3, byte a4);
        private HookWrapper<SetupDropDownList> setupDropDownList;

        private delegate byte PopulateItemList(AgentSalvage* agentSalvage);

        private HookWrapper<PopulateItemList> populateHook;

        private Dictionary<ulong, Item> ListItems = new Dictionary<ulong, Item>();
        public override FeatureType FeatureType => FeatureType.UI;

        private Overlays Overlay;

        private bool Desynthing = false;
        public override void Enable()
        {
            Overlay = new(this);
            P.Ws.AddWindow(Overlay);
            updateItemHook ??= Common.Hook<UpdateItemDelegate>("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 30 49 8B 38", UpdateItemDetour);
            updateItemHook?.Enable();
            updateListHook ??= Common.Hook<UpdateListDelegate>("40 53 56 57 48 83 EC 20 48 8B D9 49 8B F0", UpdateListDetour);
            updateListHook?.Enable();

            setupDropDownList ??= Common.Hook<SetupDropDownList>("E8 ?? ?? ?? ?? 8D 4F 55", SetupDropDownListDetour);
            setupDropDownList?.Enable();

            populateHook ??= Common.Hook<PopulateItemList>("E8 ?? ?? ?? ?? 84 C0 0F 84 ?? ?? ?? ?? 48 8B CE E8 ?? ?? ?? ?? 83 66 2C FE", PopulateDetour);
            populateHook?.Enable();

            base.Enable();
        }

        public override void Setup()
        {
            base.Setup();
        }

        private byte PopulateDetour(AgentSalvage* agentSalvage)
        {
            return populateHook.Original(agentSalvage);
        }

        private void* SetupDropDownListDetour(AtkComponentList* a1, ushort a2, byte** a3, byte a4)
        {
            return setupDropDownList.Original(a1, a2, a3, a4);
        }

        private byte UpdateListDetour(nint a1, nint a2, nint a3)
        {
            ListItems.Clear();
            return updateListHook.Original(a1, a2, a3);
        }

        private nint UpdateItemDetour(nint a1, ulong index, nint a3, ulong a4)
        {
            var retval = updateItemHook.Original(a1, index, a3, a4);
            var addon = (AddonSalvageItemSelector*)Svc.GameGui.GetAddonByName("SalvageItemSelector", 1);
            if (addon != null)
            {
                if (index > addon->ItemCount)
                {
                    return retval;
                }

                var salvageItem = addon->Items[(int)index];
                var item = InventoryManager.Instance()->GetInventoryContainer(salvageItem.Inventory)->GetInventorySlot(salvageItem.Slot);
                var itemData = Svc.Data.Excel.GetSheet<Item>().GetRow(item->ItemID);

                if (ListItems.ContainsKey(index))
                {
                    ListItems[index] = itemData;
                }
                else
                {
                    ListItems.TryAdd(index, itemData);
                }
            }
            return retval;
        }

        public override void Draw()
        {
            try
            {
                var addon = (AddonSalvageItemSelector*)Svc.GameGui.GetAddonByName("SalvageItemSelector", 1);
                if (addon != null && addon->AtkUnitBase.IsVisible)
                {
                    var node = addon->AtkUnitBase.UldManager.NodeList[10];

                    if (node == null)
                        return;

                    var position = AtkResNodeHelper.GetNodePosition(node);
                    var scale = AtkResNodeHelper.GetNodeScale(node);
                    var size = new Vector2(node->Width, node->Height) * scale;

                    ImGuiHelpers.ForceNextWindowMainViewport();
                    ImGuiHelpers.SetNextWindowPosRelativeMainViewport(position + size with { Y = 0 });

                    ImGui.PushStyleColor(ImGuiCol.WindowBg, 0);
                    float oldSize = ImGui.GetFont().Scale;
                    ImGui.GetFont().Scale *= scale.X;
                    ImGui.PushFont(ImGui.GetFont());
                    ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0f.Scale());
                    ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(3f.Scale(), 3f.Scale()));
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0f.Scale(), 0f.Scale()));
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f.Scale());
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, size);
                    ImGui.Begin($"###RepairAll{node->NodeID}", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoNavFocus
                        | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings);

                    if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Mounted])
                    {
                        ImGui.Text("You are mounted, please dismount");
                    }
                    else
                    {
                        if (!Desynthing)
                        {
                            if (ImGui.Button($"Desynth All"))
                            {
                                Desynthing = true;
                                TaskManager.Enqueue(() => YesAlready.DisableIfNeeded());
                                TaskManager.Enqueue(() => TryDesynthAll());
                                TaskManager.Enqueue(() => YesAlready.EnableIfNeeded());
                            }
                        }
                        else
                        {
                            if (ImGui.Button("Desynthing. Click to Abort."))
                            {
                                Desynthing = false;
                                TaskManager.Abort();
                                TaskManager.Enqueue(() => YesAlready.EnableIfNeeded());
                            }
                        }
                    }
                    ImGui.End();
                    ImGui.PopStyleVar(5);
                    ImGui.GetFont().Scale = oldSize;
                    ImGui.PopFont();
                    ImGui.PopStyleColor();
                }
            }
            catch
            {

            }
        }

        private void TryDesynthAll()
        {
            if (ListItems.Count > 0)
            {
                TaskManager.Enqueue(() => DesynthFirst(), "Desynthing");
                TaskManager.Enqueue(() => ConfirmDesynth(), 200, "Confirm Desynth");
                TaskManager.Enqueue(() => CloseResults(), 3000,  "Close Results");
                TaskManager.DelayNext("WaitForDelay", 400);
                TaskManager.Enqueue(() => TryDesynthAll(), "Repeat Loop");
            }
            else
            {
                TaskManager.Enqueue(() => Desynthing = false);
            }
        }

        private bool? CloseResults()
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied]) return false;
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("SalvageResult", 1);
            if (addon == null || !addon->IsVisible) return false;
            addon->Close(true);
            return true;
        }

        private bool? ConfirmDesynth()
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied]) return false;
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("SalvageDialog", 1);
            if (addon == null || !addon->IsVisible) return false;
            Callback.Fire(addon, false, 0, false);
            return true;
        }

        private bool? DesynthFirst()
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied]) return false;
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("SalvageItemSelector", 1);
            if (addon == null) return null;
            Callback.Fire(addon, false, 12, 0);
            return true;
        }

        public override void Disable()
        {
            P.Ws.RemoveWindow(Overlay);
            Overlay = null;
            updateItemHook?.Disable();
            updateListHook?.Disable();
            setupDropDownList?.Disable();
            populateHook?.Disable();
            base.Disable();
        }
    }
}
