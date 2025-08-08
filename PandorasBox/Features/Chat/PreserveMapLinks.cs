using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Hooking;
using ECommons.DalamudServices;
using Lumina.Excel.Sheets;
using PandorasBox.FeaturesSetup;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace PandorasBox.Features;

public unsafe partial class CoordsToMapLink : Feature
{
    public override string Name => "Preserve Map Links in Clipboard";

    public override string Description => "Preserves the formatting for map links so they can be interacted with after pasting.";

    public override FeatureType FeatureType => FeatureType.ChatFeature;

    [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
    private delegate nint ParseMessageDelegate(nint a, nint b);
    private Hook<ParseMessageDelegate>? parseMessageHook;

    private readonly Dictionary<string, (uint, uint)> maps = [];
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

    private readonly FieldInfo? territoryTypeIdField = typeof(MapLinkPayload).GetField("territoryTypeId", BindingFlags.NonPublic | BindingFlags.Instance);
    private readonly FieldInfo? mapIdField = typeof(MapLinkPayload).GetField("mapId", BindingFlags.NonPublic | BindingFlags.Instance);

    private readonly Dictionary<string, (uint, uint, int, int)> historyCoordinates = [];

    private nint HandleParseMessageDetour(nint a, nint b)
    {
        var ret = parseMessageHook!.Original(a, b);
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
                    Svc.Log.Verbose($"<- {BitConverter.ToString(message)}");
                    return ret;
                }
            }
            for (var i = 0; i < parsed.Payloads.Count; i++)
            {
                if (parsed.Payloads[i] is not TextPayload payload || payload.Text is null) continue;
                var match = mapLinkPattern.Match(payload.Text);
                if (!match.Success) continue;

                var mapName = match.Groups["map"].Value;
                if (unmaskedMapNames.TryGetValue(mapName, out var value))
                    mapName = value;
                var historyKey = string.Concat(mapName, match.Value.AsSpan(mapName.Length + 1));

                uint territoryId, mapId;
                int rawX, rawY;
                if (historyCoordinates.TryGetValue(historyKey, out var history))
                {
                    (territoryId, mapId, rawX, rawY) = history;
                    Svc.Log.Verbose($"recall {historyKey} => {history}");
                }
                else
                {
                    if (!maps.TryGetValue(mapName, out var mapInfo))
                    {
                        Svc.Log.Warning($"Can't find map {mapName}");
                        continue;
                    }
                    (territoryId, mapId) = mapInfo;
                    var map = Svc.Data.GetExcelSheet<Map>()!.GetRow(mapId);
                    rawX = GenerateRawPosition(float.Parse(match.Groups["x"].Value), map!.OffsetX, map!.SizeFactor);
                    rawY = GenerateRawPosition(float.Parse(match.Groups["y"].Value), map!.OffsetY, map!.SizeFactor);
                    if (match.Groups["instance"].Value != "")
                    {
                        mapId |= (match.Groups["instance"].Value[0] - 0xe0b0u) << 16;
                    }
                    history = (territoryId, mapId, rawX, rawY);
                    historyCoordinates[historyKey] = history;
                    Svc.Log.Verbose($"generate {historyKey} => {history}");
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
                Svc.Log.Verbose($"-> {BitConverter.ToString(newMessage)}");
                var messageCapacity = Marshal.ReadInt64(ret + 8);
                if (newMessage.Length + 1 > messageCapacity)
                {
                    // FIXME: should call std::string#resize(or maybe _Reallocate_grow_by) here, but haven't found the signature yet
                    Svc.Log.Info($"Reached message capacity. Aborting conversion for {historyKey}");
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
            Svc.Log.Error($"Exception on HandleParseMessageDetour. {ex}");
        }
        return ret;
    }

    private void HandleChatMessage(XivChatType type, int senderId, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        try
        {
            for (var i = 0; i < message.Payloads.Count; i++)
            {
                if (message.Payloads[i] is not MapLinkPayload payload) continue;
                if (message.Payloads[i + 6] is not TextPayload payloadText) continue;
                if (territoryTypeIdField?.GetValue(payload) is not uint { } territoryId) continue;
                if (mapIdField?.GetValue(payload) is not uint { } mapId) continue;

                var historyKey = payloadText.Text![..(payloadText.Text!.LastIndexOf(')') + 1)];
                var mapName = historyKey[..(historyKey.LastIndexOf('(') - 1)];
                if (mapName.Length == 0) continue;
                if (mapName[^1] is >= '\ue0b1' and <= '\ue0b9')
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
                Svc.Log.Verbose($"{nameof(MapLinkPayload)}: hKey:{historyKey} => h:{history}");
            }
        }
        catch (Exception ex)
        {
            Svc.Log.Error($"Exception on HandleChatMessage. {ex}");
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
        parseMessageHook ??= Svc.Hook.HookFromSignature<ParseMessageDelegate>("E8 ?? ?? ?? ?? 48 8B D0 48 8D 4C 24 ?? E8 ?? ?? ?? ?? 48 8B 44 24 ?? 48 8B CE", new(HandleParseMessageDetour));
        parseMessageHook?.Enable();

        foreach (var territoryType in Svc.Data.GetExcelSheet<TerritoryType>())
        {
            var name = territoryType.PlaceName.Value.Name.ToString();
            if (name != "" && !maps.ContainsKey(name))
            {
                maps.Add(name, (territoryType.RowId, territoryType.Map.RowId));
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

public class PreMapLinkPayload(uint territoryTypeId, uint mapId, int rawX, int rawY) : Payload
{
    public override PayloadType Type => PayloadType.AutoTranslateText;

    private readonly uint territoryTypeId = territoryTypeId;
    private readonly uint mapId = mapId;
    private readonly int rawX = rawX;
    private readonly int rawY = rawY;
    private readonly int rawZ = -30000;
    private readonly int placeNameOverride = 0;

    protected override byte[] EncodeImpl()
    {
        var sb = new Lumina.Text.SeStringBuilder();
        sb.BeginMacro(Lumina.Text.Payloads.MacroCode.Fixed)
            .AppendIntExpression(200)
            .AppendIntExpression(3)
            .AppendUIntExpression(territoryTypeId) // territory
            .AppendUIntExpression(mapId) // map or (map | (instance << 16))
            .AppendIntExpression(rawX) // x -> (int)(MathF.Round(posX, 3, MidpointRounding.AwayFromZero) * 1000)
            .AppendIntExpression(rawY) // y
            .AppendIntExpression(rawZ) // z or -30000 for no z
            .AppendIntExpression(placeNameOverride) // 0 for no override
        .EndMacro();

        return sb.ToArray();
    }

    protected override void DecodeImpl(BinaryReader reader, long endOfStream)
    {
        throw new NotImplementedException();
    }
}
