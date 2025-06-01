using ECommons.DalamudServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lumina.Excel.Sheets;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace PandorasBox.Features.Commands
{
    internal class UseItem : CommandFeature
    {
        public override string Command { get; set; } = "/puseitem";
        public override string Name => "Use Item";
        public override string Description => $@"Uses an Item from your inventory. Wrap items with multiple words in quotes, e.g /puseitem ""Smelling Salts""";

        public override List<string> Parameters => new() { "itemName" };

        protected override void OnCommand(List<string> args)
        {
            var item = Svc.Data.GetExcelSheet<Item>(Svc.ClientState.ClientLanguage).GetRow(0);
            var argName = string.Join(' ', args).Replace("\"", "");

            if (uint.TryParse(args[0].Trim(), out var id))
                item = Svc.Data.GetExcelSheet<Item>(Svc.ClientState.ClientLanguage).GetRow(id);
            else
                item = Svc.Data.GetExcelSheet<Item>().FirstOrDefault(x => x.Name.ToString().Contains(argName, StringComparison.CurrentCultureIgnoreCase));

            Use(item);
        }

        private unsafe void Use(Item item)
        {
            var id = item.RowId;
            // Aether Compass support
            if (id == 2001886)
            {
                ActionManager.Instance()->UseAction(ActionType.Action, 26988);
                return;
            }

            if (InventoryManager.Instance()->GetInventoryItemCount(id, true) > 0)
                id += 1_000_000;

            AgentInventoryContext.Instance()->UseItem(id);
        }
    }
}
