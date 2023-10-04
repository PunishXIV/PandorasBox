using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text;
using ECommons.DalamudServices;
using PandorasBox.FeaturesSetup;
using System;
using System.Collections.Generic;
using System.Linq;
using Lumina.Excel.GeneratedSheets;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons;

namespace PandorasBox.Features.Commands
{
    public unsafe class ItemLinkCommand : CommandFeature
    {
        public override string Name => "Item Link";
        public override string Command { get; set; } = "/plink";
        public override string[] Alias => new string[] { "" };

        public override List<string> Parameters => new() { "[<item name>], [<id>]" };
        public override string Description => "It's like the other item link commands, but allows searching.";

        public override FeatureType FeatureType => FeatureType.Commands;

        protected override void OnCommand(List<string> args)
        {

            var item = Svc.Data.GetExcelSheet<Item>(Svc.ClientState.ClientLanguage).GetRow(0);
            var argName = string.Join(' ', args).Replace("\"", "");

            if (uint.TryParse(args[0].Trim(), out var id))
                item = Svc.Data.GetExcelSheet<Item>(Svc.ClientState.ClientLanguage).GetRow(id);
            else
                item = Svc.Data.GetExcelSheet<Item>().FirstOrDefault(x => x.Name.RawString.Contains(argName, StringComparison.CurrentCultureIgnoreCase));

            PrintModuleMessage(GetItemLink(item.RowId));
        }

        public static SeString GetItemLink(uint id)
        {
            var item = Svc.Data.GetExcelSheet<Item>(Svc.ClientState.ClientLanguage).GetRow(id);
            if (item == null)
                return new SeString(new TextPayload($"Item#{id}"));


            var link = SeString.CreateItemLink(item, false);
            // TODO: remove in Dalamud v9
            link.Payloads.Add(UIGlowPayload.UIGlowOff);
            link.Payloads.Add(UIForegroundPayload.UIForegroundOff);
            return link;
        }
    }
}
