using Dalamud.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using System.Collections.Generic;

namespace PandorasBox.Features.UI
{
    internal unsafe class FCChestQuickDeposit : Feature
    {
        internal delegate nint AgentFreeCompanyChest_MoveFCItemDelegate(void* AgentFreeCompanyChest, InventoryType SourceInventory, uint SourceSlot, InventoryType TargetInventory, uint TargetSlot);
        internal Hook<AgentFreeCompanyChest_MoveFCItemDelegate> AgentFreeCompanyChest_MoveFCItem;

        public override string Name => "FC Chest Quick Deposit";

        public override string Description => "Adds a context menu to items whilst the FC chest is open to quickly deposit them.";

        public override FeatureType FeatureType => FeatureType.UI;

        private DalamudContextMenu contextMenu;

        private static readonly SeString DepositString = new SeString(PandoraPayload.Payloads.ToArray()).Append(new TextPayload("Deposit into FC Chest"));

        public override bool UseAutoConfig => true;
        public class Configs : FeatureConfig
        {
            [FeatureConfigOption($"Ctrl + Right Click Shortcut")]
            public bool UseShortcut = false;
        }

        public Configs Config { get; private set; }

        public override void Enable()
        {
            contextMenu = new(Svc.PluginInterface);
            contextMenu.OnOpenInventoryContextMenu += AddInventoryItem;
            AgentFreeCompanyChest_MoveFCItem ??= Svc.Hook.HookFromSignature<AgentFreeCompanyChest_MoveFCItemDelegate>("40 53 56 57 41 56 41 57 48 83 EC 30", MoveItem);
            Config = LoadConfig<Configs>() ?? new Configs();
            base.Enable();
        }

        private nint MoveItem(void* AgentFreeCompanyChest, InventoryType SourceInventory, uint SourceSlot, InventoryType TargetInventory, uint TargetSlot)
        {
            return AgentFreeCompanyChest_MoveFCItem.Original(AgentFreeCompanyChest, SourceInventory, SourceSlot, TargetInventory, TargetSlot);
        }

        private void AddInventoryItem(InventoryContextMenuOpenArgs args)
        {
            if (args.ParentAddonName == "ArmouryBoard") return;
            var item = CheckInventoryItem(args.ItemId, args.ItemHq, args.ItemAmount);
            if (item != null)
                args.AddCustomItem(item);

            if (Config.UseShortcut)
            {
                if (Svc.GameGui.GetAddonByName("FreeCompanyChest").GetAtkUnitBase(out var addon))
                {
                    if (ImGui.GetIO().KeyCtrl)
                    {
                        DepositItem(args.ItemId, addon, args.ItemHq, args.ItemAmount);
                    }
                }
            }
        }

        private unsafe InventoryContextMenuItem CheckInventoryItem(uint itemId, bool itemHq, uint itemAmount)
        {
            if (Svc.GameGui.GetAddonByName("FreeCompanyChest").GetAtkUnitBase(out var addon))
            {
                if (!addon->IsVisible) return null;
                if (addon->UldManager.NodeList[4]->IsVisible) return null;
                if (addon->UldManager.NodeList[7]->IsVisible) return null;

                if (Svc.Data.GetExcelSheet<Item>().FindFirst(x => x.RowId == itemId, out var sheetItem))
                {
                    if (sheetItem.IsUntradable) return null;
                    return new InventoryContextMenuItem(DepositString, _ => DepositItem(itemId, addon, itemHq, itemAmount), false);
                }
            }

            return null;
        }

        private unsafe void DepositItem(uint itemId, AtkUnitBase* addon, bool itemHq, uint itemAmount)
        {
            uint FCPage = (uint)InventoryType.FreeCompanyPage1;

            for (int i = 101; i >= 97; i--)
            {
                var radioButton = addon->UldManager.NodeList[i];
                if (!radioButton->IsVisible) continue;

                if (radioButton->GetAsAtkComponentNode()->Component->UldManager.NodeList[2]->IsVisible)
                {
                    FCPage = i switch
                    {
                        101 => (uint)InventoryType.FreeCompanyPage1,
                        100 => (uint)InventoryType.FreeCompanyPage2,
                        99 => (uint)InventoryType.FreeCompanyPage3,
                        98 => (uint)InventoryType.FreeCompanyPage4,
                        97 => (uint)InventoryType.FreeCompanyPage5
                    };
                }
            }

            var invManager = InventoryManager.Instance();
            InventoryType? sourceInventory = GetInventoryItemPage(itemId, itemHq, itemAmount, out short slot);
            short destSlot = CheckFCChestSlots(FCPage, itemId, itemAmount);
            if (sourceInventory != null && destSlot != -1)
            {
                AgentFreeCompanyChest_MoveFCItem.Original(UIModule.Instance()->GetAgentModule()->GetAgentByInternalId(AgentId.FreeCompanyChest), (InventoryType)sourceInventory, (uint)slot, (InventoryType)FCPage, (uint)destSlot);
            }

        }

        private unsafe short CheckFCChestSlots(uint fCPage, uint itemid, uint stack)
        {
            var invManager = InventoryManager.Instance();
            InventoryType fcPage = (InventoryType)fCPage;

            var container = invManager->GetInventoryContainer(fcPage);

            if (Svc.Data.GetExcelSheet<Item>().FindFirst(x => x.RowId == itemid, out var sheetItem))
            {
                for (var i = 0; i < container->Size; i++)
                {
                    var item = container->GetInventorySlot(i);

                    if (item->ItemID == itemid && (item->Quantity + stack) <= sheetItem.StackSize)
                    {
                        return item->Slot;
                    }
                }

                for (var i = 0; i < container->Size; i++)
                {
                    var item = container->GetInventorySlot(i);

                    if (item->ItemID == 0)
                    {
                        return item->Slot;
                    }
                }
            }

            return -1;
        }

        private unsafe InventoryType? GetInventoryItemPage(uint itemId, bool itemHq, uint itemAmount, out short slot)
        {
            var invManager = InventoryManager.Instance();

            var inventories = new List<InventoryType>
            {
                InventoryType.Inventory1,
                InventoryType.Inventory2,
                InventoryType.Inventory3,
                InventoryType.Inventory4,
            };

            foreach (var inv in inventories)
            {
                var container = invManager->GetInventoryContainer(inv);
                for (var i = 0; i < container->Size; i++)
                {
                    var item = container->GetInventorySlot(i);

                    if (item->ItemID == itemId && item->Quantity == itemAmount)
                    {
                        if ((itemHq && item->Flags == InventoryItem.ItemFlags.HQ) || (!itemHq && item->Flags != InventoryItem.ItemFlags.HQ))
                        {
                            slot = item->Slot;
                            return inv;
                        }
                    }
                }
            }

            slot = -1;
            return null;
        }

        public override void Disable()
        {
            contextMenu.OnOpenInventoryContextMenu -= AddInventoryItem;
            contextMenu?.Dispose();
            SaveConfig(Config);
            AgentFreeCompanyChest_MoveFCItem?.Dispose();
            base.Disable();
        }
    }
}
