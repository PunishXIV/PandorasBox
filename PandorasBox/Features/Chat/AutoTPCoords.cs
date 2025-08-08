using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using ECommons;
using ECommons.DalamudServices;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.IPC;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PandorasBox.Features.ChatFeature
{
    internal class AutoTPCoords : Feature
    {
        public override string Name => "Auto-Teleport to Map Coords";

        public override string Description => "Automatically teleports to the nearest aetheryte to a map link posted in chat. Requires \"Teleporter\" plugin installed.";

        public override FeatureType FeatureType => FeatureType.ChatFeature;

        public Configs Config { get; private set; } = null!;

        public override bool UseAutoConfig => false;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Include Sonar links")]
            public bool IncludeSonar = false;

            [FeatureConfigOption("Ignore <pos> flags")]
            public bool IgnorePOS = false;

            public List<ushort> FilteredChannels = new();

            [FeatureConfigOption("Disable in same zone")]
            public bool DisableSameZone = false;
        }

        public List<MapLinkMessage> MapLinkMessageList = new();
        private readonly int filterDupeTimeout = 5;

        public Lumina.Excel.ExcelSheet<Aetheryte> Aetherytes = null!;
        public Lumina.Excel.SubrowExcelSheet<MapMarker> AetherytesMap = null!;

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
                    if (Config.DisableSameZone && maplinkPayload.TerritoryType.RowId == Svc.ClientState.TerritoryType) return;
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
                    TeleportToAetheryte(newMapLinkMessage);
                }
            }

            try
            {
                foreach (var mapLink in MapLinkMessageList)
                    if (mapLink.RecordTime.Add(new TimeSpan(0, filterDupeTimeout, 0)) < DateTime.Now)
                        MapLinkMessageList.Remove(mapLink);
            }
            catch (Exception ex) { Svc.Log.Debug($"{ex}"); }
        }

        public void TeleportToAetheryte(MapLinkMessage maplinkMessage)
        {
            var aetheryteName = GetNearestAetheryte(maplinkMessage);
            if (aetheryteName != "")
            {
                Svc.Log.Debug($"Teleporting to {aetheryteName}");
                Svc.Commands.ProcessCommand($"/tp {aetheryteName}");
            }
            else
            {
                Svc.Log.Error($"Cannot find nearest aetheryte of {maplinkMessage.PlaceName}({maplinkMessage.X}, {maplinkMessage.Y}).");
            }
        }

        public string GetNearestAetheryte(MapLinkMessage maplinkMessage)
        {
            var aetheryteName = "";
            double distance = 0;
            foreach (var data in Aetherytes)
            {
                if (!data.IsAetheryte) continue;
                if (data.Territory.ValueNullable == null) continue;
                if (data.PlaceName.ValueNullable == null) continue;
                var scale = maplinkMessage.Scale;
                if (data.Territory.Value.RowId == maplinkMessage.TerritoryId)
                {
                    if (!AetherytesMap.SelectMany(x => x).TryGetFirst(m => m.DataType == 3 && m.DataKey.RowId == data.RowId, out var mapMarker))
                    {
                        Svc.Log.Error($"Cannot find aetherytes position for {maplinkMessage.PlaceName}#{data.PlaceName.Value.Name}");
                        continue;
                    }
                    var AethersX = ConvertMapMarkerToMapCoordinate(mapMarker.X, scale);
                    var AethersY = ConvertMapMarkerToMapCoordinate(mapMarker.Y, scale);
                    Svc.Log.Debug($"Aetheryte: {data.PlaceName.Value.Name} ({AethersX} ,{AethersY})");
                    var temp_distance = Math.Pow(AethersX - maplinkMessage.X, 2) + Math.Pow(AethersY - maplinkMessage.Y, 2);
                    if (aetheryteName == "" || temp_distance < distance)
                    {
                        distance = temp_distance;
                        aetheryteName = data.PlaceName.Value.Name.ToString();
                    }
                }
            }
            return aetheryteName;
        }

        private static float ConvertMapMarkerToMapCoordinate(int pos, float scale)
        {
            var num = scale / 100f;
            var rawPosition = (int)((float)(pos - 1024.0) / num * 1000f);
            return ConvertRawPositionToMapCoordinate(rawPosition, scale);
        }

        private static float ConvertRawPositionToMapCoordinate(int pos, float scale)
        {
            var num = scale / 100f;
            return (float)((((pos / 1000f * num) + 1024.0) / 2048.0 * 41.0 / num) + 1.0);
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Framework.Update += CheckForTeleporter;
            Svc.Chat.ChatMessage += OnChatMessage;
            Aetherytes = Svc.Data.GetExcelSheet<Aetheryte>();
            AetherytesMap = Svc.Data.GetSubrowExcelSheet<MapMarker>();
            base.Enable();
        }

        private void CheckForTeleporter(IFramework framework)
        {
            if (Svc.ClientState.IsLoggedIn && !TeleporterIPC.IsEnabled()) this.Disable();
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Svc.Framework.Update -= CheckForTeleporter;
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
