using Dalamud.ContextMenu;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using System.Collections.Generic;
using System.Linq;
using static ECommons.GenericHelpers;


namespace PandorasBox.Features.UI
{
    public class OpenAllCoffers : Feature
    {
        public override string Name => $@"Add ""Open All"" to Coffers";

        public override string Description => $@"Adds an ""Open All"" option to the right click menu of various items that stack and can be opened from the inventory.";

        public override FeatureType FeatureType => FeatureType.UI;

        private readonly DalamudContextMenu _contextMenu = new();

        public SeString OpenString = PandoraPayload.Append(new TextPayload("Open All"));

        public override void Enable()
        {
            _contextMenu.OnOpenGameObjectContextMenu += AddGameObjectItem;
            _contextMenu.OnOpenInventoryContextMenu += AddInventoryItem;
            base.Enable();
        }

        private void AddInventoryItem(InventoryContextMenuOpenArgs args)
        {
            var item = CheckInventoryItem(args.ItemId);
            if (item != null)
                args.AddCustomItem(item);
        }

        private InventoryContextMenuItem? CheckInventoryItem(uint itemId)
        {
            var sheetItem = Svc.Data.GetExcelSheet<Item>().Where(x => x.RowId == itemId).First();

            if (sheetItem.StackSize <= 1) return null;
            if (sheetItem.ItemAction.Row == 388 || sheetItem.ItemAction.Row == 367)
                return new InventoryContextMenuItem(OpenString, _ => TaskManager.Enqueue(() => OpenItem(itemId), true), false);

            return null;
        }

        private unsafe bool? OpenItem(uint itemId)
        {
            var invId = AgentModule.Instance()->GetAgentByInternalId(AgentId.Inventory)->GetAddonID();
            
            if (!IsInventoryFree())
            {
                return null;
            }

            if (InventoryManager.Instance()->GetInventoryItemCount(itemId) == 0)
            {
                return true;
            }
            if (!Common.GetAddonByID(invId)->IsVisible)
            {
                return null;
            }

            var inventories = new List<InventoryType>
            {
                InventoryType.Inventory1,
                InventoryType.Inventory2,
                InventoryType.Inventory3,
                InventoryType.Inventory4,
            };

            foreach (var inv in inventories)
            {
                var container = InventoryManager.Instance()->GetInventoryContainer(inv);
                for (int i = 0; i < container->Size; i++)
                {
                    var item = container->GetInventorySlot(i);

                    if (item->ItemID == itemId)
                    {
                        var ag = AgentInventoryContext.Instance();
                        ag->OpenForItemSlot(container->Type, i, AgentModule.Instance()->GetAgentByInternalId(AgentId.Inventory)->GetAddonID());
                        var contextMenu = (AtkUnitBase*)Svc.GameGui.GetAddonByName("ContextMenu", 1);
                        if (contextMenu != null)
                        {
                            var values = stackalloc AtkValue[5];
                            values[0] = new AtkValue()
                            {
                                Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                                Int = 0
                            };
                            values[1] = new AtkValue()
                            {
                                Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                                UInt = 0
                            };
                            values[2] = new AtkValue()
                            {
                                Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                                Unk = 0
                            };
                            values[3] = new AtkValue()
                            {
                                Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                                Unk = 0
                            };
                            values[4] = new AtkValue()
                            {
                                Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                                UInt = 0
                            };
                            contextMenu->FireCallback(5, values, (void*)1);

                            TaskManager.Enqueue(() => !Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Casting]);
                            TaskManager.Enqueue(() => ActionManager.Instance()->GetActionStatus(ActionType.Item, itemId, Svc.ClientState.LocalPlayer.ObjectId) == 0);
                            TaskManager.DelayNext("OpeningItem", 2200);
                            TaskManager.Enqueue(() => OpenItem(itemId));

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void AddGameObjectItem(GameObjectContextMenuOpenArgs args)
        {
            //throw new NotImplementedException();
        }

        public override void Disable()
        {
            _contextMenu.OnOpenGameObjectContextMenu -= AddGameObjectItem;
            _contextMenu.OnOpenInventoryContextMenu -= AddInventoryItem;
            base.Disable();
        }
    }
}
