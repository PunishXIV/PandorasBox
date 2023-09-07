using Dalamud.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;
using Dalamud.Utility.Signatures;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using System;
using System.Collections.Generic;
using Dalamud.Hooking;

namespace PandorasBox.Features.UI
{
    internal unsafe class FCChestQuickDeposit : Feature
    {
        internal delegate nint AgentFreeCompanyChest_MoveFCItemDelegate(void* AgentFreeCompanyChest, InventoryType SourceInventory, uint SourceSlot, InventoryType TargetInventory, uint TargetSlot);

        [Signature("40 53 56 57 41 56 41 57 48 83 EC 30")]
        internal AgentFreeCompanyChest_MoveFCItemDelegate AgentFreeCompanyChest_MoveFCItem;

        public override string Name => "FC Chest Quick Deposit";

        public override string Description => "Adds a context menu to items whilst the FC chest is open to quickly deposit them.";

        public override FeatureType FeatureType => FeatureType.UI;

        private readonly DalamudContextMenu contextMenu = new();

        private static readonly SeString DepositString = new SeString(new TextPayload("Deposit into FC Chest"));

        public override void Enable()
        {
            contextMenu.OnOpenInventoryContextMenu += AddInventoryItem;
            SignatureHelper.Initialise(this, true);
            base.Enable();
        }

        private void AddInventoryItem(InventoryContextMenuOpenArgs args)
        {
            var item = CheckInventoryItem(args.ItemId, args.ItemHq, args.ItemAmount);
            if (item != null)
                args.AddCustomItem(item);
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
            short destSlot = CheckFCChestSlots(FCPage);
            if (sourceInventory != null && destSlot != -1)
            {
                AgentFreeCompanyChest_MoveFCItem(UIModule.Instance()->GetAgentModule()->GetAgentByInternalId(AgentId.FreeCompanyChest), (InventoryType)sourceInventory, (uint)slot, (InventoryType)FCPage, (uint)destSlot);
            }

        }

        private unsafe short CheckFCChestSlots(uint fCPage)
        {
            var invManager = InventoryManager.Instance();
            InventoryType fcPage = (InventoryType)fCPage;

            var container = invManager->GetInventoryContainer(fcPage);

            for (var i = 0; i < container->Size; i++) 
            {
                var item = container->GetInventorySlot(i);

                if (item->ItemID == 0)
                {
                    return item->Slot;
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
            base.Disable();
        }
    }
}
