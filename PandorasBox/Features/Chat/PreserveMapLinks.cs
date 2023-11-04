using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using ECommons.DalamudServices;
using ECommons.Logging;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace PandorasBox.Features
{
    public unsafe partial class CoordsToMapLink : Feature
    {
        public override string Name => "Preserve Map Links in Clipboard";

        public override string Description => "Preserves the formatting for map links so they can be interacted with after pasting.";

        public override FeatureType FeatureType => FeatureType.ChatFeature;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate nint ParseMessageDelegate(nint a, nint b);
        private Hook<ParseMessageDelegate> parseMessageHook;

        private readonly Dictionary<string, (uint, uint)> maps = new();
        private readonly Dictionary<string, string> unmaskedMapNames = new()
        {
            { "狼狱演*场", "狼狱演习场" },
            { "魔**阿济兹拉", "魔大陆阿济兹拉" },
            { "玛托雅的洞*", "玛托雅的洞穴" },
            { "魔**中枢", "魔大陆中枢" },
            { "双蛇*军营", "双蛇党军营" },
            { "地衣宫演*场", "地衣宫演习场" },
            { "水晶塔演*场", "水晶塔演习场" },
            { "*泉神社", "醴泉神社" },
            { "*泉神社神道", "醴泉神社神道" },
            { "格**火山", "格鲁格火山" },
            { "**亚马乌罗提", "末日亚马乌罗提" },
            { "游末邦**", "游末邦监狱" },
        };

        [GeneratedRegex("\\uE0BB(?<map>.+?)(?<instance>[\\ue0b1-\\ue0b9])? \\( (?<x>\\d{1,2}[\\,|\\.]\\d)  , (?<y>\\d{1,2}[\\,|\\.]\\d) \\)", RegexOptions.Compiled)]
        private static partial Regex MapLinkRegex();
        private readonly Regex mapLinkPattern = MapLinkRegex();

        private readonly FieldInfo territoryTypeIdField = typeof(MapLinkPayload).GetField("territoryTypeId",
            BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly FieldInfo mapIdField = typeof(MapLinkPayload).GetField("mapId",
            BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly Dictionary<string, (uint, uint, int, int)> historyCoordinates = new();

        private nint HandleParseMessageDetour(nint a, nint b)
        {
            var ret = parseMessageHook.Original(a, b);
            try
            {
                var pMessage = Marshal.ReadIntPtr(ret);
                var length = 0;
                while (Marshal.ReadByte(pMessage, length) != 0) length++;
                var message = new byte[length];
                Marshal.Copy(pMessage, message, 0, length);

                var parsed = SeString.Parse(message);
                foreach (var payload in parsed.Payloads)
                {
                    if (payload is AutoTranslatePayload p && p.Encode()[3] == 0xC9 && p.Encode()[4] == 0x04)
                    {
                        if (Pi.IsDebugging) PluginLog.Log($"<- {BitConverter.ToString(message)}");
                        return ret;
                    }
                }
                for (var i = 0; i < parsed.Payloads.Count; i++)
                {
                    if (parsed.Payloads[i] is not TextPayload payload) continue;
                    var match = mapLinkPattern.Match(payload.Text);
                    if (!match.Success) continue;

                    var mapName = match.Groups["map"].Value;
                    if (unmaskedMapNames.ContainsKey(mapName))
                    {
                        mapName = unmaskedMapNames[mapName];
                    }
                    var historyKey = string.Concat(mapName, match.Value.AsSpan(mapName.Length + 1));

                    uint territoryId, mapId;
                    int rawX, rawY;
                    if (historyCoordinates.TryGetValue(historyKey, out var history))
                    {
                        (territoryId, mapId, rawX, rawY) = history;
                        PluginLog.Log($"recall {historyKey} => {history}");
                    }
                    else
                    {
                        if (!maps.TryGetValue(mapName, out var mapInfo))
                        {
                            PluginLog.Warning($"Can't find map {mapName}");
                            continue;
                        }
                        (territoryId, mapId) = mapInfo;
                        var map = Svc.Data.GetExcelSheet<Map>().GetRow(mapId);
                        rawX = GenerateRawPosition(float.Parse(match.Groups["x"].Value), map.OffsetX, map.SizeFactor);
                        rawY = GenerateRawPosition(float.Parse(match.Groups["y"].Value), map.OffsetY, map.SizeFactor);
                        if (match.Groups["instance"].Value != "")
                        {
                            mapId |= (match.Groups["instance"].Value[0] - 0xe0b0u) << 16;
                        }
                        history = (territoryId, mapId, rawX, rawY);
                        historyCoordinates[historyKey] = history;
                        PluginLog.Log($"generate {historyKey} => {history}");
                    }

                    var newPayloads = new List<Payload>();
                    if (match.Index > 0)
                    {
                        newPayloads.Add(new TextPayload(payload.Text[..match.Index]));
                    }
                    newPayloads.Add(new PreMapLinkPayload(territoryId, mapId, rawX, rawY));
                    if (match.Index + match.Length < payload.Text.Length)
                    {
                        newPayloads.Add(new TextPayload(payload.Text[(match.Index + match.Length)..]));
                    }
                    parsed.Payloads.RemoveAt(i);
                    parsed.Payloads.InsertRange(i, newPayloads);

                    var newMessage = parsed.Encode();
                    if (Pi.IsDebugging) PluginLog.Log($"-> {BitConverter.ToString(newMessage)}");
                    var messageCapacity = Marshal.ReadInt64(ret + 8);
                    if (newMessage.Length + 1 > messageCapacity)
                    {
                        // FIXME: should call std::string#resize(or maybe _Reallocate_grow_by) here, but haven't found the signature yet
                        PluginLog.LogError($"Reached message capacity. Aborting conversion for {historyKey}");
                        return ret;
                    }
                    Marshal.WriteInt64(ret + 16, newMessage.Length + 1);
                    Marshal.Copy(newMessage, 0, pMessage, newMessage.Length);
                    Marshal.WriteByte(pMessage, newMessage.Length, 0x00);

                    break;
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Exception on HandleParseMessageDetour. {ex}");
            }
            return ret;
        }

        private void HandleChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            try
            {
                for (var i = 0; i < message.Payloads.Count; i++)
                {
                    if (message.Payloads[i] is not MapLinkPayload payload) continue;
                    if (message.Payloads[i + 6] is not TextPayload payloadText) continue;

                    var territoryId = (uint)territoryTypeIdField.GetValue(payload);
                    var mapId = (uint)mapIdField.GetValue(payload);
                    var historyKey = payloadText.Text[..(payloadText.Text.LastIndexOf(")") + 1)];
                    var mapName = historyKey[..(historyKey.LastIndexOf("(") - 1)];
                    if ('\ue0b1' <= mapName[^1] && mapName[^1] <= '\ue0b9')
                    {
                        maps[mapName[0..^1]] = (territoryId, mapId);
                        mapId |= (mapName[^1] - 0xe0b0u) << 16;
                    }
                    else
                    {
                        maps[mapName] = (territoryId, mapId);
                    }
                    var history = (territoryId, mapId, payload.RawX, payload.RawY);
                    historyCoordinates[historyKey] = history;
                    PluginLog.Log($"memorize {historyKey} => {history}");
                    //PluginLog.Log(BitConverter.ToString(payload.Encode()));
                    //PluginLog.Log(BitConverter.ToString(payload.Encode(true)));
                }
            }
            catch (Exception ex)
            {
                PluginLog.Debug($"Exception on HandleChatMessage. {ex}");
            }
        }

        private readonly Random random = new();
        public int GenerateRawPosition(float visibleCoordinate, short offset, ushort factor)
        {
            visibleCoordinate += (float)random.NextDouble() * 0.07f;
            var scale = factor / 100.0f;
            var scaledPos = (((visibleCoordinate - 1.0f) * scale / 41.0f * 2048.0f) - 1024.0f) / scale;
            return (int)Math.Ceiling(scaledPos - offset) * 1000;
        }

        public override void Enable()
        {
            parseMessageHook ??= Svc.Hook.HookFromSignature<ParseMessageDelegate>("E8 ?? ?? ?? ?? 48 8B D0 48 8D 4C 24 30 E8 ?? ?? ?? ?? 48 8B 44 24 30 80 38 00 0F 84", new(HandleParseMessageDetour));
            parseMessageHook?.Enable();

            foreach (var territoryType in Svc.Data.GetExcelSheet<TerritoryType>())
            {
                var name = territoryType.PlaceName.Value.Name.RawString;
                if (name != "" && !maps.ContainsKey(name))
                {
                    maps.Add(name, (territoryType.RowId, territoryType.Map.Row));
                }
            }

            Svc.Chat.ChatMessage += HandleChatMessage;
            base.Enable();
        }

        public override void Disable()
        {
            parseMessageHook?.Disable();
            Svc.Chat.ChatMessage -= HandleChatMessage;
            base.Disable();
        }

        public override void Dispose()
        {
            parseMessageHook?.Dispose();
            base.Dispose();
        }
    }

    public class PreMapLinkPayload : Payload
    {
        public override PayloadType Type => PayloadType.AutoTranslateText;

        private readonly uint territoryTypeId;
        private readonly uint mapId;
        private readonly int rawX;
        private readonly int rawY;
        private readonly int rawZ;

        public PreMapLinkPayload(uint territoryTypeId, uint mapId, int rawX, int rawY)
        {
            this.territoryTypeId = territoryTypeId;
            this.mapId = mapId;
            this.rawX = rawX;
            this.rawY = rawY;
            this.rawZ = -30000;
        }

        protected override byte[] EncodeImpl()
        {
            var territoryBytes = MakeInteger(this.territoryTypeId);
            var mapBytes = MakeInteger(this.mapId);
            var xBytes = MakeInteger(unchecked((uint)this.rawX));
            var yBytes = MakeInteger(unchecked((uint)this.rawY));
            var zBytes = MakeInteger(unchecked((uint)this.rawZ));

            var chunkLen = 3 + territoryBytes.Length + mapBytes.Length + xBytes.Length + yBytes.Length + zBytes.Length;

            var bytes = new List<byte>()
            {
                START_BYTE,
                (byte)SeStringChunkType.AutoTranslateKey, (byte)chunkLen, 0xC9, 0x04
            };
            bytes.AddRange(territoryBytes);
            bytes.AddRange(mapBytes);
            bytes.AddRange(xBytes);
            bytes.AddRange(yBytes);
            bytes.AddRange(zBytes);
            bytes.Add(END_BYTE);

            return bytes.ToArray();
        }

        protected override void DecodeImpl(BinaryReader reader, long endOfStream)
        {
            throw new NotImplementedException();
        }
    }
}
