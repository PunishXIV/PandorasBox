using Dalamud.Logging;
using ECommons.DalamudServices;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Linq;

namespace PandorasBox.Helpers
{
    public static class CoordinatesHelper
    {
        public static Lumina.Excel.ExcelSheet<Aetheryte> Aetherytes = Svc.Data.GetExcelSheet<Aetheryte>(Svc.ClientState.ClientLanguage);
        public static Lumina.Excel.ExcelSheet<MapMarker> AetherytesMap = Svc.Data.GetExcelSheet<MapMarker>(Svc.ClientState.ClientLanguage);

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

        public static string GetNearestAetheryte(MapLinkMessage maplinkMessage)
        {
            var aetheryteName = "";
            double distance = 0;
            foreach (var data in Aetherytes)
            {
                if (!data.IsAetheryte) continue;
                if (data.Territory.Value == null) continue;
                if (data.PlaceName.Value == null) continue;
                var scale = maplinkMessage.Scale;
                if (data.Territory.Value.RowId == maplinkMessage.TerritoryId)
                {
                    var mapMarker = AetherytesMap.FirstOrDefault(m => m.DataType == 3 && m.DataKey == data.RowId);
                    if (mapMarker == null)
                    {
                        PluginLog.Error($"Cannot find aetherytes position for {maplinkMessage.PlaceName}#{data.PlaceName.Value.Name}");
                        continue;
                    }
                    var AethersX = ConvertMapMarkerToMapCoordinate(mapMarker.X, scale);
                    var AethersY = ConvertMapMarkerToMapCoordinate(mapMarker.Y, scale);
                    PluginLog.Log($"Aetheryte: {data.PlaceName.Value.Name} ({AethersX} ,{AethersY})");
                    var temp_distance = Math.Pow(AethersX - maplinkMessage.X, 2) + Math.Pow(AethersY - maplinkMessage.Y, 2);
                    if (aetheryteName == "" || temp_distance < distance)
                    {
                        distance = temp_distance;
                        aetheryteName = data.PlaceName.Value.Name;
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

        public static void TeleportToAetheryte(MapLinkMessage maplinkMessage)
        {
            var aetheryteName = GetNearestAetheryte(maplinkMessage);
            if (aetheryteName != "")
            {
                PluginLog.Log($"Teleporting to {aetheryteName}");
                Svc.Commands.ProcessCommand($"/tp {aetheryteName}");
            }
            else
            {
                PluginLog.Error($"Cannot find nearest aetheryte of {maplinkMessage.PlaceName}({maplinkMessage.X}, {maplinkMessage.Y}).");
            }
        }
    }
}
