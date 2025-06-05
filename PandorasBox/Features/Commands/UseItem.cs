using ECommons.DalamudServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lumina.Excel.Sheets;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ECommons.Logging;

namespace PandorasBox.Features.Commands
{
    internal class UseItem : CommandFeature
    {
        public override string Command { get; set; } = "/puseitem";
        public override string Name => "Use Item";
        public override string Description => $@"Uses an Item from your inventory.";

        public override List<string> Parameters => new() { "itemName" };

        protected unsafe override void OnCommand(List<string> args)
        {
            if (args == null || args.Count == 0)
            {
                DuoLog.Error("No item name or ID provided.");
                return;
            }

            var argName = string.Join(' ', args).Replace("\"", "").ToLower().Trim();

            // Special case for Aether Compass
            if (argName == "aether compass")
            {
                ActionManager.Instance()->UseAction(ActionType.Action, 26988);
                return;
            }

            Item item;
            EventItem keyItem;
            uint id = 0;

            // Try to parse as item ID
            if (uint.TryParse(args[0].Trim(), out id))
            {
                Svc.Data.GetExcelSheet<Item>().TryGetRow(id, out item);
                Svc.Data.GetExcelSheet<EventItem>().TryGetRow(id, out keyItem);
            }
            else
            {
                item = Svc.Data.GetExcelSheet<Item>().FirstOrDefault(x =>
                    x.Name.ToString().Contains(argName, StringComparison.CurrentCultureIgnoreCase));
                keyItem = Svc.Data.GetExcelSheet<EventItem>().FirstOrDefault(x =>
                    x.Name.ToString().Contains(argName, StringComparison.CurrentCultureIgnoreCase));
            }

            // Check if item or key item was found
            bool itemValid = item.RowId != 0 && !string.IsNullOrEmpty(item.Name.ToString());
            bool keyItemValid = keyItem.RowId != 0 && !string.IsNullOrEmpty(keyItem.Name.ToString());

            if (!itemValid && !keyItemValid)
            {
                DuoLog.Error($@"Item ""{argName}"" not found.");
                return;
            }

            // Prefer normal item if found, otherwise use key item
            if (itemValid)
            {
                Use(item);
            }
            else if (keyItemValid)
            {
                Use(keyItem);
            }
        }

        private unsafe void Use(EventItem? keyItem)
        {
            var id = keyItem?.RowId ?? 0;
            AgentInventoryContext.Instance()->UseItem(id, InventoryType.KeyItems);
        }

        private unsafe void Use(Item? item)
        {
            var id = item?.RowId ?? 0;
            if (InventoryManager.Instance()->GetInventoryItemCount(id, true) > 0)
                id += 1_000_000;

            AgentInventoryContext.Instance()->UseItem(id);
        }
    }
}
