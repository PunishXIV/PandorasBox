// Credit entirely to Bluefissure: https://github.com/Bluefissure/NoKillPlugin

using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Logging;
using ECommons.DalamudServices;
using PandorasBox.FeaturesSetup;
using System;
using System.Runtime.InteropServices;

namespace PandorasBox.Features.Other
{
    public unsafe class NoKill : Feature
    {
        public override string Name => "Prevent Lobby Error Crashes";

        public override string Description => "Prevents the game from killing itself when it gets a lobby error";

        public override FeatureType FeatureType => FeatureType.Other;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("SkipAuthErrors")]
            public bool SkipAuthError = true;

            [FeatureConfigOption("QueueMode: Use for lobby errors in queues")]
            public bool QueueMode = false;

            [FeatureConfigOption("SaferMode: Filters invalid messages that may crash the client")]
            public bool SaferMode = false;
        }

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => true;

        internal IntPtr StartHandler;
        internal IntPtr LoginHandler;
        internal IntPtr LobbyErrorHandler;
        internal IntPtr DecodeSeStringHandler;
        // internal IntPtr RequestHandler;
        // internal IntPtr ResponseHandler;
        private delegate Int64 StartHandlerDelegate(Int64 a1, Int64 a2);
        private delegate Int64 LoginHandlerDelegate(Int64 a1, Int64 a2);
        private delegate char LobbyErrorHandlerDelegate(Int64 a1, Int64 a2, Int64 a3);
        private delegate void DecodeSeStringHandlerDelegate(Int64 a1, Int64 a2, Int64 a3, Int64 a4);
        // private delegate char RequestHandlerDelegate(Int64 a1, int a2);
        // private delegate void ResponseHandlerDelegate(Int64 a1, Int64 a2, Int64 a3, int a4);
        private Hook<StartHandlerDelegate> StartHandlerHook;
        private Hook<LoginHandlerDelegate> LoginHandlerHook;
        //private Hook<DecodeSeStringHandlerDelegate> DecodeSeStringHandlerHook;
        private Hook<LobbyErrorHandlerDelegate> LobbyErrorHandlerHook;
        // private Regex rx = new Regex(@"2E .. .. .. (?!03)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        /*
        private Hook<RequestHandlerDelegate> RequestHandlerHook;
        private Hook<ResponseHandlerDelegate> ResponseHandlerHook;
        */

        private void RunFeature(Framework framework)
        {
            return;
        }

        private Int64 StartHandlerDetour(Int64 a1, Int64 a2)
        {
            var a1_88 = (UInt16)Marshal.ReadInt16(new IntPtr(a1 + 88));
            var a1_456 = Marshal.ReadInt32(new IntPtr(a1 + 456));
            PluginLog.Log($"Start a1_456:{a1_456}");
            if (a1_456 != 0 && Config.QueueMode)
            {
                Marshal.WriteInt32(new IntPtr(a1 + 456), 0);
                PluginLog.Log($"a1_456: {a1_456} => 0");
            }
            return this.StartHandlerHook.Original(a1, a2);
        }
        private Int64 LoginHandlerDetour(Int64 a1, Int64 a2)
        {
            var a1_2165 = Marshal.ReadByte(new IntPtr(a1 + 2165));
            PluginLog.Log($"Login a1_2165:{a1_2165}");
            if (a1_2165 != 0 && Config.QueueMode)
            {
                Marshal.WriteByte(new IntPtr(a1 + 2165), 0);
                PluginLog.Log($"a1_2165: {a1_2165} => 0");
            }
            return this.LoginHandlerHook.Original(a1, a2);
        }

        private bool isValidSeString(byte[] managedArray, int len)
        {
            int i = 0;
            while (i < len && i + 1 < len)
            {
                if (managedArray[i] == 0x2E)
                {
                    var sz = managedArray[i + 1];
                    if (i + 1 + sz >= len) return false;
                    if (managedArray[i + 1 + sz] != 0x03) return false;
                }
                i++;
            }
            return true;
        }

        /*
        private void DecodeSeStringHandlerDetour(Int64 a1, Int64 a2, Int64 a3, Int64 a4)
        {
            if (!Config.SaferMode)
            {
                this.DecodeSeStringHandlerHook.Original(a1, a2, a3, a4);
                return;
            }

            try
            {
                var a2_byte = Marshal.ReadByte(new IntPtr(a2));
                if (a2_byte == 2)
                {
                    var a2pointer = new IntPtr(a2);
                    var maxlen = 256;
                    int len = 0;
                    while (len < maxlen && Marshal.ReadByte(a2pointer + len) != 0) len++;
                    byte[] managedArray = new byte[len];
                    Marshal.Copy(a2pointer, managedArray, 0, len);
                    var bytesString = BitConverter.ToString(managedArray).Replace("-", " ");
                    if (managedArray[0] == 0x02 && managedArray[1] == 0x2E)
                    {
                        if (!isValidSeString(managedArray, len))
                        {
                            PluginLog.Log($"invalid auto trans array:{bytesString}");
                            return;
                        }else
                        {
                            PluginLog.Log($"valid auto trans array:{bytesString}");
                        }
                    }
                }
            } catch (Exception e)
            {
                PluginLog.Log("Don't crash");
                PluginLog.Log(e.StackTrace);
            }
            this.DecodeSeStringHandlerHook.Original(a1, a2, a3, a4);
        }
        */

        private char LobbyErrorHandlerDetour(Int64 a1, Int64 a2, Int64 a3)
        {
            IntPtr p3 = new IntPtr(a3);
            var t1 = Marshal.ReadByte(p3);
            var v4 = ((t1 & 0xF) > 0) ? (uint)Marshal.ReadInt32(p3 + 8) : 0;
            UInt16 v4_16 = (UInt16)(v4);
            PluginLog.Log($"LobbyErrorHandler a1:{a1} a2:{a2} a3:{a3} t1:{t1} v4:{v4_16}");
            if (v4 > 0)
            {
                // this.Gui.ConfigWindow.Visible = true; // why was this here?
                if (v4_16 == 0x332C && Config.SkipAuthError) // Auth failed
                {
                    PluginLog.Log($"Skip Auth Error");
                }
                else
                {
                    Marshal.WriteInt64(p3 + 8, 0x3E80); // server connection lost
                    // 0x3390: maintenance
                    v4 = ((t1 & 0xF) > 0) ? (uint)Marshal.ReadInt32(p3 + 8) : 0;
                    v4_16 = (UInt16)(v4);
                }
            }
            PluginLog.Log($"After LobbyErrorHandler a1:{a1} a2:{a2} a3:{a3} t1:{t1} v4:{v4_16}");
            return this.LobbyErrorHandlerHook.Original(a1, a2, a3);
        }
        /*
        private char RequestHandlerDetour(Int64 a1, int a2)
        {
            IntPtr p1 = new IntPtr(a1 + 2100);
            IntPtr p2 = new IntPtr(a1 + 2082);
            var t1 = Marshal.ReadByte(p1);
            var t2 = Marshal.ReadInt16(p2);
            PluginLog.Log($"RequestHandlerDetour a1:{a1:X} *(a1+2100):{t1} *(a1+2082):{t2} a2:{a2}");
            return this.RequestHandlerHook.Original(a1, a2);
        }
        private void ResponseHandlerDetour(Int64 a1, Int64 a2, Int64 a3, int a4)
        {
            UInt32 A1 = (UInt32)Marshal.ReadInt32(new IntPtr(a1));
            this.ResponseHandlerHook.Original(a1, a2, a3, a4);
            Int32 A2 = Marshal.ReadInt32(new IntPtr(a2));
            Int64 A3 = Marshal.ReadInt64(new IntPtr(a3));
            Int32 v14 = Marshal.ReadInt32(new IntPtr(a3 + 8));
            PluginLog.Log($"ResponseHandlerDetour a1:{a1:X} a2:{a2:X} a3:{a3:X} A3:{A3:X} a4:{a4}");
            return;
        }
        */

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Framework.Update += RunFeature;
            this.LobbyErrorHandler = Svc.SigScanner.ScanText("40 53 48 83 EC 30 48 8B D9 49 8B C8 E8 ?? ?? ?? ?? 8B D0");
            this.LobbyErrorHandlerHook = new Hook<LobbyErrorHandlerDelegate>(
                LobbyErrorHandler,
                new LobbyErrorHandlerDelegate(LobbyErrorHandlerDetour)
            );
            try
            {
                this.StartHandler = Svc.SigScanner.ScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? B2 01 49 8B CC");
            }
            catch (Exception)
            {
                this.StartHandler = Svc.SigScanner.ScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? B2 01 49 8B CD");
            }
            this.StartHandlerHook = new Hook<StartHandlerDelegate>(
                StartHandler,
                new StartHandlerDelegate(StartHandlerDetour)
            );
            this.LoginHandler = Svc.SigScanner.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 0F B6 81 ?? ?? ?? ?? 40 32 FF");
            this.LoginHandlerHook = new Hook<LoginHandlerDelegate>(
                LoginHandler,
                new LoginHandlerDelegate(LoginHandlerDetour)
            );
            /*
            this.DecodeSeStringHandler = SigScanner.ScanText("E8 ?? ?? ?? ?? 8B 5E 60 48 8D 4C 24 ??");
            this.DecodeSeStringHandlerHook = new Hook<DecodeSeStringHandlerDelegate>(
                DecodeSeStringHandler,
                new DecodeSeStringHandlerDelegate(DecodeSeStringHandlerDetour)
            );
            */
            /*
            this.RequestHandler = SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8B 9C 24 ?? ?? ?? ?? 48 8B B4 24 ?? ?? ?? ?? 83 7F 20 00");
            this.RequestHandlerHook = new Hook<RequestHandlerDelegate>(
                RequestHandler,
                new RequestHandlerDelegate(RequestHandlerDetour)
            );
            this.ResponseHandler = SigScanner.ScanText("E8 ?? ?? ?? ?? 48 8D 85 ?? ?? ?? ?? 48 3B F8");
            this.ResponseHandlerHook = new Hook<ResponseHandlerDelegate>(
                ResponseHandler,
                new ResponseHandlerDelegate(ResponseHandlerDetour)
            );
            */

            this.LobbyErrorHandlerHook.Enable();
            this.StartHandlerHook.Enable();
            this.LoginHandlerHook.Enable();
            //ChatGui.ChatMessage += OnChatMessage;
            //this.DecodeSeStringHandlerHook.Enable();
            //this.RequestHandlerHook.Enable();
            //this.ResponseHandlerHook.Enable();
            //GameNetwork.NetworkMessage += OnNetwork;
            base.Enable();
        }


        public override void Disable()
        {
            SaveConfig(Config);
            Svc.Framework.Update -= RunFeature;
            this.LobbyErrorHandlerHook.Disable();
            this.StartHandlerHook.Disable();
            this.LoginHandlerHook.Disable();
            base.Disable();
        }

    }
}
