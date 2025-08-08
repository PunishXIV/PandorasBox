using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using PandorasBox.FeaturesSetup;
using System;
using System.Collections.Generic;
using System.Linq;
using static ECommons.ExcelServices.TerritoryIntendedUseEnum;

namespace PandorasBox.Features.ChatFeature;

internal class AutoOpenCoords : Feature
{
    public override string Name => "Auto-Open Map Coords";

    public override string Description => "Automatically opens the map to coordinates posted in chat.";

    public override FeatureType FeatureType => FeatureType.ChatFeature;

    public Configs Config { get; private set; } = null!;

    public override bool UseAutoConfig => false;

    public class Configs : FeatureConfig
    {
        [FeatureConfigOption("Include Sonar links")]
        public bool IncludeSonar = false;

        [FeatureConfigOption("Set <flag> without opening the map")]
        public bool DontOpenMap = false;

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

    private void OnChatMessage(XivChatType type, int senderId, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        var hasMapLink = false;
        float coordX = 0;
        float coordY = 0;
        float scale = 100;
        MapLinkPayload maplinkPayload = null!;
        foreach (var payload in message.Payloads)
        {
            if (payload is MapLinkPayload mapLinkload)
            {
                maplinkPayload = mapLinkload;
                hasMapLink = true;
                // float fudge = 0.05f;
                scale = mapLinkload.TerritoryType.Value.Map.Value.SizeFactor;
                // coordX = ConvertRawPositionToMapCoordinate(mapLinkload.RawX, scale) - fudge;
                // coordY = ConvertRawPositionToMapCoordinate(mapLinkload.RawY, scale) - fudge;
                coordX = mapLinkload.XCoord;
                coordY = mapLinkload.YCoord;
                Svc.Log.Debug($"TerritoryId: {mapLinkload.TerritoryType.RowId} {mapLinkload.PlaceName} ({coordX} ,{coordY})");
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

            var alreadyInList = MapLinkMessageList.Any(w =>
            {
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
                PlaceMapMarker(newMapLinkMessage);
            }
        }

        try
        {
            foreach (var mapLink in MapLinkMessageList.ToList())
                if (mapLink.RecordTime.Add(new TimeSpan(0, filterDupeTimeout, 0)) < DateTime.Now)
                    MapLinkMessageList.Remove(mapLink);
        }
        catch (Exception ex) { ex.Log(); }
    }

    public unsafe void PlaceMapMarker(MapLinkMessage maplinkMessage)
    {
        if (Player.TerritoryIntendedUse is not (City_Area or Open_World or Inn or Starting_Area or Housing_Instances or Residential_Area or Chocobo_Square or Gold_Saucer or Diadem or Barracks))
        {
            Svc.Log.Debug($"Not in a city area, skipping map marker placement.");
            return;
        }
        Svc.Log.Debug($"Viewing {maplinkMessage.Text}");
        var map = Svc.Data.GetExcelSheet<TerritoryType>().GetRow(maplinkMessage.TerritoryId).Map;
        var maplink = new MapLinkPayload(maplinkMessage.TerritoryId, map.RowId, maplinkMessage.X, maplinkMessage.Y);

        if (Config.DontOpenMap)
        {
            AgentMap.Instance()->SetFlagMapMarker(maplinkMessage.TerritoryId, map.RowId, maplink.RawX, maplink.RawY);
        }
        else
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

    protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
    {
        if (ImGui.Checkbox("Include Sonar links", ref Config.IncludeSonar)) hasChanged = true;
        if (ImGui.Checkbox("Ignore <pos> flags", ref Config.IgnorePOS)) hasChanged = true;
        //ImGui.Checkbox("Set <flag> without opening the map", ref Config.DontOpenMap);

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

public class MapLinkMessage
{
    public static MapLinkMessage Empty => new(0, string.Empty, string.Empty, 0, 0, 100, 0, string.Empty, DateTime.Now);

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
