using Dalamud.Game.Network;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using ECommons.DalamudServices;
using Newtonsoft.Json;
using PandorasBox.FeaturesSetup;
using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace PandorasBox.Features.Other
{
    public unsafe class FishNotify : Feature
    {
        public override string Name => "Fish Notify";

        public override string Description => "Play a sound when a fish is hooked.";

        public override FeatureType FeatureType => FeatureType.Other;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Chat Alerts")]
            public bool ChatAlerts = true;

            [FeatureConfigOption("Light Tugs")]
            public bool LightTugs = true;

            [FeatureConfigOption("Medium Tugs")]
            public bool MediumTugs = true;

            [FeatureConfigOption("Heavy Tugs")]
            public bool HeavyTugs = true;
        }

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => true;

        private int expectedOpCode = -1;
        private uint fishCount = 0;
        private static readonly SoundPlayer player = new SoundPlayer();

        public class OpcodeRegion
        {
            public string? Version { get; set; }
            public string? Region { get; set; }
            public Dictionary<string, List<OpcodeList>>? Lists { get; set; }
        }

        public class OpcodeList
        {
            public string? Name { get; set; }
            public ushort Opcode { get; set; }
        }

        private void ExtractOpCode(Task<string> task)
        {
            try
            {
                var regions = JsonConvert.DeserializeObject<List<OpcodeRegion>>(task.Result);
                if (regions == null)
                {
                    PluginLog.Warning("No regions found in opcode list");
                    return;
                }

                var region = regions.Find(r => r.Region == "Global");
                if (region == null || region.Lists == null)
                {
                    PluginLog.Warning("No global region found in opcode list");
                    return;
                }

                if (!region.Lists.TryGetValue("ServerZoneIpcType", out List<OpcodeList>? serverZoneIpcTypes) || serverZoneIpcTypes == null)
                {
                    PluginLog.Warning("No ServerZoneIpcType in opcode list");
                    return;
                }

                var eventPlay = serverZoneIpcTypes.Find(opcode => opcode.Name == "EventPlay");
                if (eventPlay == null)
                {
                    PluginLog.Warning("No EventPlay opcode in ServerZoneIpcType");
                    return;
                }

                expectedOpCode = eventPlay.Opcode;
                PluginLog.Debug($"Found EventPlay opcode {expectedOpCode:X4}");
            }
            catch (Exception e)
            {
                PluginLog.Error(e, "Could not download/extract opcodes: {}", e.Message);
            }
        }

        private void OnNetworkMessage(IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction)
        {
            if (direction != NetworkMessageDirection.ZoneDown || opCode != expectedOpCode)
                return;

            var data = new byte[32];
            Marshal.Copy(dataPtr, data, 0, data.Length);

            int eventId = BitConverter.ToInt32(data, 8);
            short scene = BitConverter.ToInt16(data, 12);
            int param5 = BitConverter.ToInt32(data, 28);

            // Fishing event?
            if (eventId != 0x00150001)
                return;

            // Fish hooked?
            if (scene != 5)
                return;

            switch (param5)
            {

                case 0x124:
                    // light tug (!)
                    if (!Config.LightTugs) return;
                    ++fishCount;
                    PlaySound(Resources.Info);
                    SendChatAlert("light");
                    break;

                case 0x125:
                    // medium tug (!!)
                    if (!Config.MediumTugs) return;
                    ++fishCount;
                    PlaySound(Resources.Alert);
                    SendChatAlert("medium");
                    break;

                case 0x126:
                    // heavy tug (!!!)
                    if (!Config.HeavyTugs) return;
                    ++fishCount;
                    PlaySound(Resources.Alarm);
                    SendChatAlert("heavy");
                    break;

                default:
                    StopSound();
                    break;
            }
        }

        private void SendChatAlert(string size)
        {
            if (!Config.ChatAlerts) return;

            SeString message = new SeStringBuilder()
            .AddUiForeground(45)
            .Append("[Pandora's Box]")
            .AddUiForeground(62)
            .Append(" [FishNotify]")
            .AddUiForegroundOff()
            .Append($" You hook a fish with a ")
            .AddUiForeground(576)
            .Append(size)
            .AddUiForegroundOff()
            .Append(" bite.")
            .Build();
            Svc.Chat.Print(message);
        }

        public static void PlaySound(Stream input)
        {
            lock (player)
            {
                StopSound();

                player.Stream = input;
                player.Play();
            }
        }

        public static void StopSound()
        {
            lock (player)
            {
                player.Stop();
                player.Stream = null;
            }
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.GameNetwork.NetworkMessage += OnNetworkMessage;
            var client = new HttpClient();
            client.GetStringAsync("https://raw.githubusercontent.com/karashiiro/FFXIVOpcodes/master/opcodes.min.json")
            .ContinueWith(ExtractOpCode);
            base.Enable();
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Svc.GameNetwork.NetworkMessage -= OnNetworkMessage;
            base.Disable();
        }
    }

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources
    {
        private static global::System.Resources.ResourceManager resourceMan;
        private static global::System.Globalization.CultureInfo resourceCulture;
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() { }

        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager
        {
            get
            {
                if (object.ReferenceEquals(resourceMan, null))
                {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("FishNotify.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }

        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture
        {
            get
            {
                return resourceCulture;
            }
            set
            {
                resourceCulture = value;
            }
        }

        internal static System.IO.UnmanagedMemoryStream Alarm
        {
            get
            {
                return ResourceManager.GetStream("Alarm", resourceCulture);
            }
        }

        internal static System.IO.UnmanagedMemoryStream Alert
        {
            get
            {
                return ResourceManager.GetStream("Alert", resourceCulture);
            }
        }

        internal static System.IO.UnmanagedMemoryStream Info
        {
            get
            {
                return ResourceManager.GetStream("Info", resourceCulture);
            }
        }
    }
}