using PandorasBox.FeaturesSetup;
using ECommons.DalamudServices;
using System.Linq;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Text;
using System;
using Dalamud.Game.Text.SeStringHandling;
using Lumina.Excel.GeneratedSheets;
using Dalamud.Logging;
using System.Collections.Generic;
using ImGuiNET;
using PandorasBox.Helpers;

namespace PandorasBox.Features.ChatFeature
{
    internal class AutoTPCoords : Feature
    {
        public override string Name => "Auto-Teleport to Map Coords";

        public override string Description => "Automatically teleports to the nearest aetheryte to a map link posted in chat";

        public override FeatureType FeatureType => FeatureType.ChatFeature;

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => false;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Include Sonar links")]
            public bool IncludeSonar = false;

            [FeatureConfigOption("Ignore <pos> flags")]
            public bool IgnorePOS = false;

            public List<ushort> FilteredChannels = new();
        }

        public List<MapLinkMessage> MapLinkMessageList = new();
        private readonly int filterDupeTimeout = 5;

        public List<XivChatType> HiddenChatType = new()
        {
            XivChatType.None,
            XivChatType.CustomEmote,
            XivChatType.StandardEmote,
            XivChatType.SystemMessage,
            XivChatType.SystemError,
            XivChatType.GatheringSystemMessage,
            XivChatType.ErrorMessage,
            XivChatType.RetainerSale
        };

        private void OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            var hasMapLink = false;
            float coordX = 0;
            float coordY = 0;
            float scale = 100;
            MapLinkPayload maplinkPayload = null;
            foreach (var payload in message.Payloads)
            {
                if (payload is MapLinkPayload mapLinkload)
                {
                    maplinkPayload = mapLinkload;
                    hasMapLink = true;
                    // float fudge = 0.05f;
                    scale = mapLinkload.TerritoryType.Map.Value.SizeFactor;
                    // coordX = ConvertRawPositionToMapCoordinate(mapLinkload.RawX, scale) - fudge;
                    // coordY = ConvertRawPositionToMapCoordinate(mapLinkload.RawY, scale) - fudge;
                    coordX = mapLinkload.XCoord;
                    coordY = mapLinkload.YCoord;
                    PluginLog.Log($"TerritoryId: {mapLinkload.TerritoryType.RowId} {mapLinkload.PlaceName} ({coordX} ,{coordY})");
                }
            }

            var messageText = message.TextValue;
            if (hasMapLink)
            {
                var newMapLinkMessage = new MapLinkMessage(
                        (ushort)type,
                        sender.TextValue,
                        messageText,
                        coordX,
                        coordY,
                        scale,
                        maplinkPayload.TerritoryType.RowId,
                        maplinkPayload.PlaceName,
                        DateTime.Now
                    );

                var filteredOut = false;
                if (sender.TextValue.ToLower() == "sonar" && !Config.IncludeSonar)
                    filteredOut = true;

                var alreadyInList = MapLinkMessageList.Any(w => {
                    var sameText = w.Text == newMapLinkMessage.Text;
                    var timeoutMin = new TimeSpan(0, filterDupeTimeout, 0);
                    if (newMapLinkMessage.RecordTime < w.RecordTime + timeoutMin)
                    {
                        var sameX = (int)(w.X * 10) == (int)(newMapLinkMessage.X * 10);
                        var sameY = (int)(w.Y * 10) == (int)(newMapLinkMessage.Y * 10);
                        var sameTerritory = w.TerritoryId == newMapLinkMessage.TerritoryId;
                        return sameTerritory && sameX && sameY;
                    }
                    return sameText;
                });

                if (alreadyInList) filteredOut = true;
                if (!filteredOut && Config.FilteredChannels.IndexOf((ushort)type) != -1) filteredOut = true;
                if (!filteredOut)
                {
                    if (Config.IgnorePOS && newMapLinkMessage.Text.Contains("Z:")) return;

                    MapLinkMessageList.Add(newMapLinkMessage);
                    CoordinatesHelper.TeleportToAetheryte(newMapLinkMessage);
                }
            }

            try
            {
                foreach (var mapLink in MapLinkMessageList)
                    if (mapLink.RecordTime.Add(new TimeSpan(0, filterDupeTimeout, 0)) < DateTime.Now)
                        MapLinkMessageList.Remove(mapLink);
            }
            catch (Exception ex) { PluginLog.Log($"{ex}"); }
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Chat.ChatMessage += OnChatMessage;
            base.Enable();
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Svc.Chat.ChatMessage -= OnChatMessage;
            base.Disable();
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
        {
            if (ImGui.Checkbox("Include Sonar links", ref Config.IncludeSonar)) hasChanged = true;
            if (ImGui.Checkbox("Ignore <pos> flags", ref Config.IgnorePOS)) hasChanged = true;

            if (ImGui.CollapsingHeader("Channel Filters (Whitelist)"))
            {
                ImGui.Indent();
                foreach (ushort chatType in Enum.GetValues(typeof(XivChatType)))
                {
                    if (HiddenChatType.IndexOf((XivChatType)chatType) != -1) continue;

                    var chatTypeName = Enum.GetName(typeof(XivChatType), chatType);
                    var checkboxClicked = Config.FilteredChannels.IndexOf(chatType) == -1;

                    if (ImGui.Checkbox(chatTypeName + "##filter", ref checkboxClicked))
                    {
                        hasChanged = true;
                        Config.FilteredChannels = Config.FilteredChannels.Distinct().ToList();

                        if (checkboxClicked)
                        {
                            if (Config.FilteredChannels.IndexOf(chatType) != -1)
                                Config.FilteredChannels.Remove(chatType);
                        }
                        else if (Config.FilteredChannels.IndexOf(chatType) == -1)
                        {
                            Config.FilteredChannels.Add(chatType);
                        }

                        Config.FilteredChannels = Config.FilteredChannels.Distinct().ToList();
                        Config.FilteredChannels.Sort();
                    }
                }
                ImGui.Unindent();
            }
        };
    }

    
}
