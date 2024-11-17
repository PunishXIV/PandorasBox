// Credit entirely to Bluefissure: https://github.com/Bluefissure/NoKillPlugin

using Dalamud.Hooking;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using PandorasBox.FeaturesSetup;
using System;
using System.Runtime.InteropServices;

namespace PandorasBox.Features.Other
{
    public unsafe class NoKill : Feature
    {
        public override string Name => "Prevent Lobby Error Crashes";

        public override string Description => "Prevents the game from closing itself when it gets a lobby error";

        public override FeatureType FeatureType => FeatureType.Other;

        public override bool FeatureDisabled => true;

        public override string DisabledReason => "Feature not working, requires update.";

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Skip Authentication Errors")]
            public bool SkipAuthError = true;

            [FeatureConfigOption(name: "Queue Mode: Use for lobby errors in queues", Disabled = true)]
            public bool QueueMode = false;

            [FeatureConfigOption("Safer Mode: Filters invalid messages that may crash the client")]
            public bool SaferMode = false;

            [FeatureConfigOption("Try to Close Error Automatically")]
            public bool CloseAutomatically = false;
        }

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => true;

        internal IntPtr StartHandler;
        internal IntPtr LoginHandler;
        internal IntPtr LobbyErrorHandler;
        private delegate Int64 StartHandlerDelegate(Int64 a1, Int64 a2);
        private delegate Int64 LoginHandlerDelegate(Int64 a1, Int64 a2);
        private delegate char LobbyErrorHandlerDelegate(Int64 a1, Int64 a2, Int64 a3);
        private delegate void DecodeSeStringHandlerDelegate(Int64 a1, Int64 a2, Int64 a3, Int64 a4);
        private Hook<StartHandlerDelegate> startHandlerHook;
        private Hook<LoginHandlerDelegate> loginHandlerHook;
        private Hook<LobbyErrorHandlerDelegate> lobbyErrorHandlerHook;


        private Int64 StartHandlerDetour(Int64 a1, Int64 a2)
        {
            //var a1_88 = (UInt16)Marshal.ReadInt16(new IntPtr(a1 + 88));
            //var a1_456 = Marshal.ReadInt32(new IntPtr(a1 + 456));
            //Svc.Log.Debug($"Start a1_456:{a1_456}");
            //if (a1_456 != 0 && Config.QueueMode)
            //{
            //    Marshal.WriteInt32(new IntPtr(a1 + 456), 0);
            //    Svc.Log.Debug($"a1_456: {a1_456} => 0");
            //}
            return this.startHandlerHook.Original(a1, a2);
        }
        private Int64 LoginHandlerDetour(Int64 a1, Int64 a2)
        {
            var a1_2165 = Marshal.ReadByte(new IntPtr(a1 + 2165));
            Svc.Log.Debug($"Login a1_2165:{a1_2165}");
            if (a1_2165 != 0 && Config.QueueMode)
            {
                Marshal.WriteByte(new IntPtr(a1 + 2165), 0);
                Svc.Log.Debug($"a1_2165: {a1_2165} => 0");
            }
            return this.loginHandlerHook.Original(a1, a2);
        }

        private char LobbyErrorHandlerDetour(Int64 a1, Int64 a2, Int64 a3)
        {
            var p3 = new IntPtr(a3);
            var t1 = Marshal.ReadByte(p3);
            var v4 = ((t1 & 0xF) > 0) ? (uint)Marshal.ReadInt32(p3 + 8) : 0;
            var v4_16 = (UInt16)(v4);
            Svc.Log.Debug($"LobbyErrorHandler a1:{a1} a2:{a2} a3:{a3} t1:{t1} v4:{v4_16}");
            if (v4 > 0)
            {
                if (v4_16 == 0x332C && Config.SkipAuthError) // Auth failed
                {
                    Svc.Log.Debug($"Skip Auth Error");
                }
                else
                {
                    Marshal.WriteInt64(p3 + 8, 0x3E80); // server connection lost
                    // 0x3390: maintenance
                    v4 = ((t1 & 0xF) > 0) ? (uint)Marshal.ReadInt32(p3 + 8) : 0;
                    v4_16 = (UInt16)(v4);
                }
            }
            Svc.Log.Debug($"After LobbyErrorHandler a1:{a1} a2:{a2} a3:{a3} t1:{t1} v4:{v4_16}");

            return this.lobbyErrorHandlerHook.Original(a1, a2, a3);
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            lobbyErrorHandlerHook ??= Svc.Hook.HookFromSignature<LobbyErrorHandlerDelegate>("40 53 48 83 EC 30 48 8B D9 49 8B C8 E8 ?? ?? ?? ?? 8B D0", LobbyErrorHandlerDetour);
            this.StartHandler = this.StartHandler = Svc.SigScanner.ScanText("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 49 8B CD E8 ?? ?? ?? ?? 45 88 66 08");
            this.startHandlerHook = Svc.Hook.HookFromAddress<StartHandlerDelegate>(StartHandler, new StartHandlerDelegate(StartHandlerDetour));
            this.LoginHandler = Svc.SigScanner.ScanText("40 55 53 56 57 41 54 48 8D AC 24 ?? ?? ?? ?? 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 85 ?? ?? ?? ?? 8B B1 ?? ?? ?? ??");
            this.loginHandlerHook = Svc.Hook.HookFromAddress<LoginHandlerDelegate>(LoginHandler, new LoginHandlerDelegate(LoginHandlerDetour));

            this.lobbyErrorHandlerHook.Enable();
            this.startHandlerHook.Enable();
            this.loginHandlerHook.Enable();

            Svc.Framework.Update += CheckDialogue;

            base.Enable();
        }

        private void CheckDialogue(IFramework framework)
        {
            if (!Config.CloseAutomatically) return;
            if (Svc.GameGui.GetAddonByName("Dialogue") != IntPtr.Zero && !Svc.Condition.Any())
            {
                var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Dialogue");
                if (!addon->IsVisible) return;

                WindowsKeypress.SendKeypress(ECommons.Interop.LimitedKeys.NumPad0);
            }
        }

        public override void Disable()
        {
            SaveConfig(Config);
            this.lobbyErrorHandlerHook?.Disable();
            this.startHandlerHook?.Disable();
            this.loginHandlerHook?.Disable();
            Svc.Framework.Update -= CheckDialogue;

            base.Disable();
        }

        public override void Dispose()
        {
            this.lobbyErrorHandlerHook?.Dispose();
            this.startHandlerHook?.Dispose();
            this.loginHandlerHook?.Dispose();
            base.Dispose();
        }
    }
}
