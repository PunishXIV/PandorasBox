using Dalamud.Game;
using Dalamud.Logging;
using Dalamud.Plugin.Ipc.Exceptions;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Loader;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using System.Collections.Generic;
using System.Linq;

namespace PandorasBox.Features.UI
{
    public unsafe class MergeStacks : Feature
    {
        public override string Name => "Automatically merge stacks of same items";

        public override string Description => "When you open your inventory, the plugin will try and pull all stacks of the same item together.";

        public override FeatureType FeatureType => FeatureType.UI;

        public List<InventorySlot> inventorySlots = new List<InventorySlot>();

        private bool InventoryOpened = false;

        private Dictionary<uint, Item> Sheet;

        public class InventorySlot
        {
            public InventoryType Container { get; set; }

            public short Slot { get; set; }

            public uint ItemID { get; set; }

            public bool ItemHQ { get; set; }
        }

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Sort after merging")]
            public bool SortAfter = false;
        }

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => true;


        public override void Enable()
        {
            Sheet = Svc.Data.GetExcelSheet<Item>().ToDictionary(x => x.RowId, x => x);
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        private void RunFeature(Framework framework)
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied | Dalamud.Game.ClientState.Conditions.ConditionFlag.Casting])
            {
                TaskManager.Abort();
                return;
            }
            var id = AgentModule.Instance()->GetAgentByInternalId(AgentId.Inventory)->GetAddonID();
            var addon = Common.GetAddonByID(id);
            if (addon == null) return;
            if (addon->IsVisible && !InventoryOpened)
            {
                InventoryOpened = true;
                inventorySlots.Clear();
                InventoryManager* inv = InventoryManager.Instance();
                var inv1 = inv->GetInventoryContainer(InventoryType.Inventory1);
                for (int i = 1; i <= inv1->Size; i++)
                {
                    var item = inv1->GetInventorySlot(i - 1);
                    if (item->Flags.HasFlag(InventoryItem.ItemFlags.Collectable)) continue;
                    if (item->Quantity == Sheet[item->ItemID].StackSize || item->ItemID == 0)
                        continue;
                    InventorySlot slot = new InventorySlot() { Container = InventoryType.Inventory1, ItemID = item->ItemID, Slot = item->Slot, ItemHQ = item->Flags.HasFlag(InventoryItem.ItemFlags.HQ) };
                    inventorySlots.Add(slot);
                }
                var inv2 = inv->GetInventoryContainer(InventoryType.Inventory2);
                for (int i = 1; i <= inv2->Size; i++)
                {
                    var item = inv2->GetInventorySlot(i - 1);
                    if (item->Flags.HasFlag(InventoryItem.ItemFlags.Collectable)) continue;
                    if (item->Quantity == Sheet[item->ItemID].StackSize || item->ItemID == 0)
                        continue;
                    InventorySlot slot = new InventorySlot() { Container = InventoryType.Inventory2, ItemID = item->ItemID, Slot = item->Slot, ItemHQ = item->Flags.HasFlag(InventoryItem.ItemFlags.HQ) };
                    inventorySlots.Add(slot);
                }
                var inv3 = inv->GetInventoryContainer(InventoryType.Inventory3);
                for (int i = 1; i <= inv3->Size; i++)
                {
                    var item = inv3->GetInventorySlot(i - 1);
                    if (item->Flags.HasFlag(InventoryItem.ItemFlags.Collectable)) continue;
                    if (item->Quantity == Sheet[item->ItemID].StackSize || item->ItemID == 0)
                        continue;
                    InventorySlot slot = new InventorySlot() { Container = InventoryType.Inventory3, ItemID = item->ItemID, Slot = item->Slot, ItemHQ = item->Flags.HasFlag(InventoryItem.ItemFlags.HQ) };
                    inventorySlots.Add(slot);
                }
                var inv4 = inv->GetInventoryContainer(InventoryType.Inventory4);
                for (int i = 1; i <= inv4->Size; i++)
                {
                    var item = inv4->GetInventorySlot(i - 1);
                    if (item->Flags.HasFlag(InventoryItem.ItemFlags.Collectable)) continue;
                    if (item->Quantity == Sheet[item->ItemID].StackSize || item->ItemID == 0)
                        continue;
                    InventorySlot slot = new InventorySlot() { Container = InventoryType.Inventory4, ItemID = item->ItemID, Slot = item->Slot, ItemHQ = item->Flags.HasFlag(InventoryItem.ItemFlags.HQ) };
                    inventorySlots.Add(slot);
                }

                foreach (var item in inventorySlots.GroupBy(x => new { x.ItemID, x.ItemHQ }).Where(x => x.Count() > 1))
                {
                    var firstSlot = item.First();
                    for (int i = 1; i < item.Count(); i++)
                    {
                        var slot = item.ToList()[i];
                        inv->MoveItemSlot(slot.Container, slot.Slot, firstSlot.Container, firstSlot.Slot, 1);
                    }
                }

                if (inventorySlots.GroupBy(x => new { x.ItemID, x.ItemHQ }).Any(x => x.Count() > 1) && Config.SortAfter)
                {
                    TaskManager.DelayNext("Sort", 100);
                    TaskManager.Enqueue(() => Chat.Instance.SendMessage("/isort condition inventory id"));
                    TaskManager.Enqueue(() => Chat.Instance.SendMessage("/isort execute inventory"));
                }
            }
            else if (!addon->IsVisible)
            {
                InventoryOpened = false;
            }
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Sheet = null;
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }
    }
}
