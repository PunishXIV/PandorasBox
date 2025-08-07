using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PandorasBox.Features.UI
{
    internal unsafe class FCChestQuickDeposit : Feature
    {
        public delegate nint MoveItemDelegate(void* agent, InventoryType srcInv, uint srcSlot, InventoryType dstInv, uint dstSlot);
        public MoveItemDelegate? MoveItem;
        internal nint address;

        public override string Name => "FC Chest Quick Deposit";

        public override bool FeatureDisabled => false;

        public override string DisabledReason => "Issues with crashing";

        public override string Description => "Adds a context menu to items whilst the FC chest is open to quickly deposit them.";

        public override FeatureType FeatureType => FeatureType.UI;

        private IContextMenu contextMenu;

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
            if (Svc.SigScanner.TryScanText("40 53 55 56 57 41 57 48 83 EC ?? 45 33 FF", out address))
            {
                contextMenu = Svc.ContextMenu;
                contextMenu.OnMenuOpened += AddInventoryItem;
                MoveItem = Marshal.GetDelegateForFunctionPointer<MoveItemDelegate>(address);
                Config = LoadConfig<Configs>() ?? new Configs();
                base.Enable();
            }
        }

        private void AddInventoryItem(IMenuOpenedArgs args)
        {
            if (args.AddonName == "ArmouryBoard") return;
            if (args.MenuType != ContextMenuType.Inventory) return;
            var invItem = ((MenuTargetInventory)args.Target).TargetItem!.Value;

            var item = CheckInventoryItem(invItem.ItemId, invItem.IsHq, invItem.Quantity);
            if (item != null)
                args.AddMenuItem(item);

            if (Config.UseShortcut)
            {
                if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("FreeCompanyChest", out var addon))
                {
                    if (ImGui.GetIO().KeyCtrl)
                    {
                        DepositItem(invItem.ItemId, addon, invItem.IsHq, invItem.Quantity);
                    }
                }
            }
        }

        private unsafe MenuItem CheckInventoryItem(uint ItemId, bool itemHq, int itemAmount)
        {
            
            if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("FreeCompanyChest", out var addon))
            {
                if (!addon->IsVisible) return null;
                if (addon->UldManager.NodeList[4]->IsVisible()) return null;
                if (addon->UldManager.NodeList[7]->IsVisible()) return null;

                if (Svc.Data.GetExcelSheet<Item>()!.FindFirst(x => x.RowId == ItemId, out var sheetItem))
                {
                    if (sheetItem.IsUntradable) return null;
                    var menu = new MenuItem();
                    menu.Name = DepositString;
                    menu.OnClicked += _ => DepositItem(ItemId, addon, itemHq, itemAmount);
                    return menu;
                }
            }

            return null;
        }

        private unsafe void DepositItem(uint ItemId, AtkUnitBase* addon, bool itemHq, int itemAmount)
        {
            uint FCPage = (uint)InventoryType.FreeCompanyPage1;

            for (int i = 101; i >= 97; i--)
            {
                var radioButton = addon->UldManager.NodeList[i];
                if (!radioButton->IsVisible()) continue;

                if (radioButton->GetAsAtkComponentNode()->Component->UldManager.NodeList[2]->IsVisible())
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
            InventoryType? sourceInventory = GetInventoryItemPage(ItemId, itemHq, itemAmount, out short slot);
            short destSlot = CheckFCChestSlots(FCPage, ItemId, itemAmount, itemHq);
            if (sourceInventory != null && destSlot != -1)
            {
                try
                {
                    MoveItem(UIModule.Instance()->GetAgentModule()->GetAgentByInternalId(AgentId.FreeCompanyChest), (InventoryType)sourceInventory, (uint)slot, (InventoryType)FCPage, (uint)destSlot);
                }
                catch (Exception ex)
                {
                    ex.Log();
                }
            }

        }

        private unsafe short CheckFCChestSlots(uint fCPage, uint ItemId, int stack, bool itemHq)
        {
            var invManager = InventoryManager.Instance();
            InventoryType fcPage = (InventoryType)fCPage;

            var container = invManager->GetInventoryContainer(fcPage);

            if (Svc.Data.GetExcelSheet<Item>()!.FindFirst(x => x.RowId == ItemId, out var sheetItem))
            {
                for (var i = 0; i < container->Size; i++)
                {
                    var item = container->GetInventorySlot(i);
                    if ((item->Flags.HasFlag(InventoryItem.ItemFlags.HighQuality) && !itemHq) || (!item->Flags.HasFlag(InventoryItem.ItemFlags.HighQuality) && itemHq)) continue;

                    if (item->ItemId == ItemId && (item->Quantity + stack) <= sheetItem.StackSize)
                    {
                        return item->Slot;
                    }
                }

                for (var i = 0; i < container->Size; i++)
                {
                    var item = container->GetInventorySlot(i);

                    if (item->ItemId == 0)
                    {
                        return item->Slot;
                    }
                }
            }

            return -1;
        }

        private unsafe InventoryType? GetInventoryItemPage(uint ItemId, bool itemHq, int itemAmount, out short slot)
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

                    if (item->ItemId == ItemId && item->Quantity == itemAmount)
                    {
                        if ((itemHq && item->Flags == InventoryItem.ItemFlags.HighQuality) || (!itemHq && item->Flags != InventoryItem.ItemFlags.HighQuality))
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
            contextMenu.OnMenuOpened -= AddInventoryItem;
            SaveConfig(Config);
            base.Disable();
        }

        public override void Dispose()
        {
            base.Dispose();
        }


        public const int SatisfactionSupplyItemIdx = 0x54;
        public const int SatisfactionSupplyItem1Id = 0x80 + 1 * 0x3C;
        public const int SatisfactionSupplyItem2Id = 0x80 + 2 * 0x3C;
        public const int ContentsInfoDetailContextItemId = 0x17CC;
        public const int RecipeNoteContextItemId = 0x398;
        public const int AgentItemContextItemId = 0x28;
        public const int GatheringNoteContextItemId = 0xA0;
        public const int ItemSearchContextItemId = 0x1740;
        public const int ChatLogContextItemId = 0x948;

        public const int SubmarinePartsMenuContextItemId = 0x54;
        public const int ShopExchangeItemContextItemId = 0x54;
        public const int ShopContextMenuItemId = 0x54;
        public const int ShopExchangeCurrencyContextItemId = 0x54;
        public const int HWDSupplyContextItemId = 0x38C;
        public const int GrandCompanySupplyListContextItemId = 0x54;
        public const int GrandCompanyExchangeContextItemId = 0x54;

        private uint? GetGameObjectItemId(IMenuOpenedArgs args)
        {
            var item = args.AddonName switch
            {
                null => HandleNulls(),
                "Shop" => GetObjectItemId("Shop", ShopContextMenuItemId),
                "GrandCompanySupplyList" => GetObjectItemId("GrandCompanySupplyList", GrandCompanySupplyListContextItemId),
                "GrandCompanyExchange" => GetObjectItemId("GrandCompanyExchange", GrandCompanyExchangeContextItemId),
                "ShopExchangeCurrency" => GetObjectItemId("ShopExchangeCurrency", ShopExchangeCurrencyContextItemId),
                "SubmarinePartsMenu" => GetObjectItemId("SubmarinePartsMenu", SubmarinePartsMenuContextItemId),
                "ShopExchangeItem" => GetObjectItemId("ShopExchangeItem", ShopExchangeItemContextItemId),
                "ContentsInfoDetail" => GetObjectItemId("ContentsInfo", ContentsInfoDetailContextItemId),
                "RecipeNote" => GetObjectItemId("RecipeNote", RecipeNoteContextItemId),
                "RecipeTree" => GetObjectItemId(AgentById(AgentId.RecipeItemContext), AgentItemContextItemId),
                "RecipeMaterialList" => GetObjectItemId(AgentById(AgentId.RecipeItemContext), AgentItemContextItemId),
                "RecipeProductList" => GetObjectItemId(AgentById(AgentId.RecipeItemContext), AgentItemContextItemId),
                "GatheringNote" => GetObjectItemId("GatheringNote", GatheringNoteContextItemId),
                "ItemSearch" => GetObjectItemId(args.AgentPtr, ItemSearchContextItemId),
                "ChatLog" => GetObjectItemId("ChatLog", ChatLogContextItemId),
                _ => null,
            };
            if (item == null)
            {
                var guiHoveredItem = Svc.GameGui.HoveredItem;
                if (guiHoveredItem >= 2000000 || guiHoveredItem == 0) return null;
                item = (uint)guiHoveredItem % 500_000;
            }

            return item;
        }

        private uint GetObjectItemId(uint itemId)
        {
            if (itemId > 500000)
                itemId -= 500000;

            return itemId;
        }

        private unsafe uint? GetObjectItemId(IntPtr agent, int offset)
            => agent != IntPtr.Zero ? GetObjectItemId(*(uint*)(agent + offset)) : null;

        private uint? GetObjectItemId(string name, int offset)
            => GetObjectItemId(Svc.GameGui.FindAgentInterface(name), offset);

        private unsafe uint? HandleSatisfactionSupply()
        {
            var agent = Svc.GameGui.FindAgentInterface("SatisfactionSupply");
            if (agent == IntPtr.Zero)
                return null;

            var itemIdx = *(byte*)(agent + SatisfactionSupplyItemIdx);
            return itemIdx switch
            {
                1 => GetObjectItemId(*(uint*)(agent + SatisfactionSupplyItem1Id)),
                2 => GetObjectItemId(*(uint*)(agent + SatisfactionSupplyItem2Id)),
                _ => null,
            };
        }
        private unsafe uint? HandleHWDSupply()
        {
            var agent = Svc.GameGui.FindAgentInterface("HWDSupply");
            if (agent == IntPtr.Zero)
                return null;

            return GetObjectItemId(*(uint*)(agent + HWDSupplyContextItemId));
        }

        private uint? HandleNulls()
        {
            var itemId = HandleSatisfactionSupply() ?? HandleHWDSupply();
            return itemId;
        }

        private unsafe IntPtr AgentById(AgentId id)
        {
            var uiModule = (UIModule*)Svc.GameGui.GetUIModule().Address;
            var agents = uiModule->GetAgentModule();
            var agent = agents->GetAgentByInternalId(id);
            return (IntPtr)agent;
        }

    }
}
