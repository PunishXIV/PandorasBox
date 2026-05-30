using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using System.Collections.Generic;


namespace PandorasBox.Features.UI
{
    public class OpenAllCoffers : Feature
    {
        public override string Name => $@"Add ""Open All"" to Coffers";

        public override string Description => $@"Adds an ""Open All"" option to the right click menu of various items that stack and can be opened from the inventory.";

        public override FeatureType FeatureType => FeatureType.UI;

        private IContextMenu contextMenu = null!;

        private static readonly SeString OpenString = new SeString(PandoraPayload.Payloads.ToArray()).Append(new TextPayload("Open All"));

        public override void Enable()
        {
            contextMenu = Svc.ContextMenu;
            contextMenu.OnMenuOpened += AddInventoryItem;
            base.Enable();
        }

        private void AddInventoryItem(IMenuOpenedArgs args)
        {
            if (args.MenuType != ContextMenuType.Inventory) return;
            var argItem = ((MenuTargetInventory)args.Target).TargetItem!.Value;
            var item = CheckInventoryItem(argItem.ItemId);
            if (item != null)
                args.AddMenuItem(item);
        }

        private MenuItem? CheckInventoryItem(uint ItemId)
        {
            if (Svc.Data.GetExcelSheet<Item>().FindFirst(x => x.RowId == ItemId, out var sheetItem))
            {
                if (sheetItem.StackSize <= 1) return null;
                if (sheetItem.ItemAction.RowId is 388 or 367 or 2462)
                {
                    var menuItem = new MenuItem();
                    menuItem.Name = OpenString;
                    menuItem.OnClicked += _ => TaskManager.Enqueue(() => OpenItem(ItemId));
                    return menuItem;
                }
            }

            return null;
        }

        private unsafe bool? OpenItem(uint ItemId)
        {
            var invId = AgentModule.Instance()->GetAgentByInternalId(AgentId.Inventory)->GetAddonId();

            if (IsMoving())
            {
                return null;
            }

            if (!IsInventoryFree())
            {
                return null;
            }

            if (InventoryManager.Instance()->GetInventoryItemCount(ItemId) == 0)
            {
                return true;
            }

            TaskManager.Enqueue(() => !Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Casting]);
            TaskManager.Enqueue(() => ActionManager.Instance()->GetActionStatus(ActionType.Item, ItemId, Svc.Objects.LocalPlayer.GameObjectId) == 0);
            TaskManager.Enqueue(() => ActionManager.Instance()->AnimationLock == 0);
            TaskManager.Enqueue(() => OpenItem(ItemId));

            AgentInventoryContext.Instance()->UseItem(ItemId);

            return true;
        }

        public override void Disable()
        {
            contextMenu.OnMenuOpened -= AddInventoryItem;
            base.Disable();
        }
    }
}
