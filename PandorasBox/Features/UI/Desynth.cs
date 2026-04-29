using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Hooking;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using PandorasBox.UI;
using static ECommons.GenericHelpers;

namespace PandorasBox.Features.UI
{
    public unsafe class Desynth : Feature
    {
        public override string Name => "Desynth All";

        public override string Description => "Adds a button to the desynthesis window to desynth all from the current dropdown. (Disclaimer: Pandora takes no responsibility for the loss of any Ultimate weapons or other rare items. Please use responsibly.)";

        private delegate IntPtr UpdateItemDelegate(IntPtr a1, ulong index, IntPtr a3, ulong a4);
        private Hook<AddonSalvageItemSelector.Delegates.PopulateSalvageItemListItem> updateItemHook = null!;

        private Dictionary<int, Item> ListItems { get; set; } = [];
        public override FeatureType FeatureType => FeatureType.UI;

        private Overlays Overlay { get; set; }

        private bool Desynthing { get; set; } = false;
        public override void Enable()
        {
            Overlay = new(this);
            updateItemHook ??= Svc.Hook.HookFromAddress<AddonSalvageItemSelector.Delegates.PopulateSalvageItemListItem>((nint)AddonSalvageItemSelector.MemberFunctionPointers.PopulateSalvageItemListItem, UpdateItemDetour);
            updateItemHook?.Enable();
            base.Enable();
        }

        private void UpdateItemDetour(AddonSalvageItemSelector* thisPtr, int index, AtkResNode** nodeList, AtkComponentListItemRenderer* listItemRenderer)
        {
            if (TryGetAddonByName<AddonSalvageItemSelector>("SalvageItemSelector", out var addon))
            {
                if (index > addon->ItemCount)
                {
                    updateItemHook.Original(thisPtr, index, nodeList, listItemRenderer);
                    return;
                }

                var salvageItem = addon->Items[index];
                var item = InventoryManager.Instance()->GetInventoryContainer(salvageItem.Inventory)->GetInventorySlot(salvageItem.Slot);
                var itemData = Svc.Data.Excel.GetSheet<Item>().GetRow(item->ItemId);

                if (ListItems.ContainsKey(index))
                    ListItems[index] = itemData;
                else
                    ListItems.TryAdd(index, itemData);
            }
            updateItemHook.Original(thisPtr, index, nodeList, listItemRenderer);
        }

        public override bool DrawConditions() => Svc.GameGui.GetAddonByName("SalvageItemSelector", 1) != nint.Zero;

        public override void Draw()
        {
            try
            {
                if (TryGetAddonByName<AddonSalvageItemSelector>("SalvageItemSelector", out var addon))
                {
                    var node = addon->AtkUnitBase.GetNodeById(6);

                    if (node == null)
                        return;

                    var position = AtkResNodeHelper.GetNodePosition(node);
                    var scale = AtkResNodeHelper.GetNodeScale(node);
                    var size = new Vector2(node->Width, node->Height) * scale;

                    ImGuiHelpers.ForceNextWindowMainViewport();
                    var pos = position + size with { Y = 0 };
                    pos.X += 12f;
                    ImGuiHelpers.SetNextWindowPosRelativeMainViewport(pos);

                    ImGui.PushStyleColor(ImGuiCol.WindowBg, 0);
                    var oldSize = ImGui.GetFont().Scale;
                    ImGui.GetFont().Scale *= scale.X;
                    ImGui.PushFont(ImGui.GetFont());
                    ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0f.Scale());
                    ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(3f.Scale(), 3f.Scale()));
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0f.Scale(), 0f.Scale()));
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f.Scale());
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, size);
                    ImGui.Begin($"###DesynthAll{node->NodeId}", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoNavFocus
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
                                TaskManager.Enqueue(YesAlready.Lock);
                                TaskManager.Enqueue(TryDesynthAll);
                            }
                        }
                        else
                        {
                            if (ImGui.Button("Desynthing. Click to Abort."))
                            {
                                Desynthing = false;
                                TaskManager.Abort();
                                TaskManager.Enqueue(YesAlready.Unlock);
                            }
                        }
                    }
                    ImGui.End();
                    ImGui.PopStyleVar(5);
                    ImGui.GetFont().Scale = oldSize;
                    ImGui.PopFont();
                    ImGui.PopStyleColor();
                }
                else
                {
                    Desynthing = false;
                    TaskManager.Abort();
                    TaskManager.Enqueue(YesAlready.Unlock);
                }
            }
            catch
            {

            }
        }

        private void TryDesynthAll()
        {
            if (TryGetAddonByName<AddonSalvageItemSelector>("SalvageItemSelector", out var addon))
            {
                if (addon->ItemCount > 0)
                {
                    TaskManager.Enqueue(DesynthFirst, "Desynthing");
                    TaskManager.EnqueueWithTimeout(ConfirmDesynth, 2000, "Confirm Desynth");
                    TaskManager.EnqueueWithTimeout(CloseResults, 9000, "Close Results");
                    TaskManager.EnqueueDelay(500);
                    TaskManager.Enqueue(TryDesynthAll, "Repeat Loop");
                }
                else
                {
                    Desynthing = false;
                    YesAlready.Unlock();
                }
            }
        }

        private bool? CloseResults()
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied]) return false;
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("SalvageResult", 1).Address;
            if (addon == null || !addon->IsVisible) return false;
            addon->Close(true);
            return true;
        }

        private bool? ConfirmDesynth()
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied]) return false;
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("SalvageDialog", 1).Address;
            if (addon == null || !addon->IsVisible) return false;
            ECommons.Automation.Callback.Fire(addon, false, 0, false);
            return Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied39];
        }

        private static bool? DesynthFirst()
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied]) return false;
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("SalvageItemSelector", 1).Address;
            if (addon == null) return null;
            ECommons.Automation.Callback.Fire(addon, false, 12, 0);
            return true;
        }

        public override void Disable()
        {
            P.Ws.RemoveWindow(Overlay);
            Overlay = null;
            updateItemHook?.Disable();
            base.Disable();
        }

        public override void Dispose() => updateItemHook?.Dispose();
    }
}
