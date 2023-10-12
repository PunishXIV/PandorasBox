using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using System;
using System.Collections.Generic;

namespace PandorasBox.Features.Commands
{
    public unsafe class TeleportToFlag : CommandFeature
    {
        public override string Name => "Teleport to Flag";
        public override string Command { get; set; } = "/ptpf";
        public override string Description => "Teleports you to the aetheryte nearest your <flag>";

        public override FeatureType FeatureType => FeatureType.Commands;
        protected override void OnCommand(List<string> args)
        {
            var fauxMapLinkMessage = new CoordinatesHelper.MapLinkMessage(
                (ushort)0,
                "",
                "",
                AgentMap.Instance()->FlagMapMarker.XFloat,
                AgentMap.Instance()->FlagMapMarker.YFloat,
                100,
                AgentMap.Instance()->FlagMapMarker.TerritoryId,
                "",
                DateTime.Now
            );
            CoordinatesHelper.TeleportToAetheryte(fauxMapLinkMessage);
        }
    }
}
