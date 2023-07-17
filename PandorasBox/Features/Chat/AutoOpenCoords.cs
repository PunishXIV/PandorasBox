using PandorasBox.Features;
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

namespace PandorasBox.Features.ChatFeature
{
    internal class AutoOpenCoords : Feature
    {
        public override string Name => "Auto-Open Map Coords";

        public override string Description => "Automatically opens the map to coordinates posted in chat.";

        public override FeatureType FeatureType => FeatureType.ChatFeature;

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => true;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Include Sonar links")]
            public bool IncludeSonar = false;
        }

        public List<MapLinkMessage> MapLinkMessageList = new List<MapLinkMessage>();
        private readonly int filterDupeTimeout = 5;

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
                //if (!filteredOut && Config.FilteredChannels.IndexOf((ushort)type) != -1) filteredOut = true;
                if (!filteredOut)
                {
                    MapLinkMessageList.Add(newMapLinkMessage);
                    PlaceMapMarker(newMapLinkMessage);
                }
            }

            foreach (MapLinkMessage mapLink in MapLinkMessageList)
                if (mapLink.RecordTime.Add(new TimeSpan(0, filterDupeTimeout, 0)) < DateTime.Now)
                    MapLinkMessageList.Remove(mapLink);
        }

        public static void PlaceMapMarker(MapLinkMessage maplinkMessage)
        {
            PluginLog.Log($"Viewing {maplinkMessage.Text}");
            var map = Svc.Data.GetExcelSheet<TerritoryType>().GetRow(maplinkMessage.TerritoryId).Map;
            var maplink = new MapLinkPayload(maplinkMessage.TerritoryId, map.Row, maplinkMessage.X, maplinkMessage.Y);
            Svc.GameGui.OpenMapWithMapLink(maplink);
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
    }

    public class MapLinkMessage
    {
        public static MapLinkMessage Empty => new MapLinkMessage(0, string.Empty, string.Empty, 0, 0, 100, 0, string.Empty, DateTime.Now);

        public ushort ChatType;
        public string Sender;
        public string Text;
        public float X;
        public float Y;
        public float Scale;
        public uint TerritoryId;
        public string PlaceName;
        public DateTime RecordTime;

        public MapLinkMessage(ushort chatType, string sender, string text, float x, float y, float scale, uint territoryId, string placeName, DateTime recordTime)
        {
            ChatType = chatType;
            Sender = sender;
            Text = text;
            X = x;
            Y = y;
            Scale = scale;
            TerritoryId = territoryId;
            PlaceName = placeName;
            RecordTime = recordTime;
        }
    }
}
