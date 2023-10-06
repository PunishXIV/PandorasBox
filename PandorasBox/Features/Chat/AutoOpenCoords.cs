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
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Dalamud.Hooking;
using System.Diagnostics;
using System.Reflection;

namespace PandorasBox.Features.ChatFeature
{
    internal class AutoOpenCoords : Feature
    {
        public override string Name => "Auto-Open Map Coords";

        public override string Description => "Automatically opens the map to coordinates posted in chat.";

        public override FeatureType FeatureType => FeatureType.ChatFeature;

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => false;

        GameIntegration GI;

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
                    PlaceMapMarker(newMapLinkMessage);
                }
            }

            try
            {
                foreach (var mapLink in MapLinkMessageList)
                    if (mapLink.RecordTime.Add(new TimeSpan(0, filterDupeTimeout, 0)) < DateTime.Now)
                        MapLinkMessageList.Remove(mapLink);
            }
            catch (Exception ex) { PluginLog.Log($"{ex}");  }
        }

        public unsafe void PlaceMapMarker(MapLinkMessage maplinkMessage)
        {
            PluginLog.Log($"Viewing {maplinkMessage.Text}");
            var map = Svc.Data.GetExcelSheet<TerritoryType>().GetRow(maplinkMessage.TerritoryId).Map;
            var maplink = new MapLinkPayload(maplinkMessage.TerritoryId, map.Row, maplinkMessage.X, maplinkMessage.Y);

            if (Config.DontOpenMap)
            {
                var agent = AgentMap.Instance();
                GI.SetFlagMarker(agent, maplinkMessage.TerritoryId, map.Row, maplink.RawX, maplink.RawY, 60561);
            }
            else
                Svc.GameGui.OpenMapWithMapLink(maplink);
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Chat.ChatMessage += OnChatMessage;
            GI = new();
            base.Enable();
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Svc.Chat.ChatMessage -= OnChatMessage;
            GI.Dispose();
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

    public unsafe class GameIntegration : IDisposable
    {
        private delegate void SetFlagMarkerDelegate(AgentMap* agent, uint territoryId, uint mapId, float mapX, float mapY, uint iconId);
        private readonly Hook<SetFlagMarkerDelegate>? setFlagMarkerHook;

        public GameIntegration()
        {
            setFlagMarkerHook ??= Svc.Hook.HookFromAddress<SetFlagMarkerDelegate>((nint)AgentMap.Addresses.SetFlagMapMarker.Value, SetFlagMarker);
        }
        //internal void SetFlagMarker(AgentMap* agent, uint territoryId, uint mapId, float mapX, float mapY, uint iconId)
        internal void SetFlagMarker(AgentMap* agent, uint territoryId, uint mapId, float mapX, float mapY, uint iconId) => Safety.ExecuteSafe(() =>
        {
            PluginLog.Debug($"SetFlagMarker : {mapX} {mapY}");

            setFlagMarkerHook!.Original(agent, territoryId, mapId, mapX, mapY, iconId);
        }, "Exception during SetFlagMarker");

        public void Dispose()
        {
            setFlagMarkerHook?.Dispose();
        }
    }

    public static class Safety
    {
        public static void ExecuteSafe(System.Action action, string? message = null)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                var trace = new StackTrace().GetFrame(1);
                var callingAssembly = Assembly.GetCallingAssembly().GetName().Name;

                if (trace is not null)
                {
                    var callingClass = trace.GetMethod()?.DeclaringType;
                    var callingName = trace.GetMethod()?.Name;

                    PluginLog.Error($"Exception Source: {callingAssembly} :: {callingClass} :: {callingName}");
                }

                PluginLog.Error(exception, message ?? "Caught Exception Safely");
            }
        }
    }
}
