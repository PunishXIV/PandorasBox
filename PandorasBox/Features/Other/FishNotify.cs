using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using ECommons.DalamudServices;
using ImGuiNET;
using PandorasBox.FeaturesSetup;
using System;

namespace PandorasBox.Features.Other
{
    public unsafe class FishNotify : Feature
    {
        public override string Name => "Fish Notify";

        public override string Description => "Play a sound or send a chat message when a fish is hooked.";

        public override FeatureType FeatureType => FeatureType.Other;

        public class Configs : FeatureConfig
        {
            public bool LightTugs = true;
            public bool LightChat = true;
            public bool PlayLightSound = true;

            public bool StrongTugs = true;
            public bool StrongChat = true;
            public bool PlayStrongSound = true;

            public bool LegendaryTugs = true;
            public bool LegendaryChat = true;
            public bool PlayLegendarySound = true;
        }

        public Configs Config { get; private set; }

        public enum FishingState : byte
        {
            None = 0,
            PoleOut = 1,
            PullPoleIn = 2,
            Quit = 3,
            PoleReady = 4,
            Bite = 5,
            Reeling = 6,
            Waiting = 8,
            Waiting2 = 9,
        }

        public enum BiteType : byte
        {
            Unknown = 0,
            Weak = 36,
            Strong = 37,
            Legendary = 38,
            None = 255,
        }

        public static SeTugType TugType { get; set; } = null!;
        private readonly EventFramework eventFramework = new(Svc.SigScanner);
        private Helpers.AudioHandler audioHandler { get; set; }
        private bool hasHooked = false;

        private void RunFeature(Framework framework)
        {
            if (!Svc.Condition[ConditionFlag.Fishing]) { hasHooked = false; return; }

            var state = eventFramework.FishingState;
            if (state != FishingState.Bite) return;

            if (!hasHooked)
                switch (TugType.Bite)
                {
                    case BiteType.Weak:
                        hasHooked = true;
                        if (Config.LightChat) TaskManager.Enqueue(() => SendChatAlert("light"));
                        if (Config.PlayLightSound && CheckIsSfxEnabled()) audioHandler.PlaySound(Helpers.AudioTrigger.Light);
                        break;

                    case BiteType.Strong:
                        hasHooked = true;
                        if (Config.StrongChat) TaskManager.Enqueue(() => SendChatAlert("strong"));
                        if (Config.PlayStrongSound && CheckIsSfxEnabled()) audioHandler.PlaySound(Helpers.AudioTrigger.Strong);
                        break;

                    case BiteType.Legendary:
                        hasHooked = true;
                        if (Config.LegendaryChat) TaskManager.Enqueue(() => SendChatAlert("legendary"));
                        if (Config.PlayLegendarySound && CheckIsSfxEnabled()) audioHandler.PlaySound(Helpers.AudioTrigger.Legendary);
                        break;

                    default:
                        break;
                }
            return;
        }

        private unsafe bool CheckIsSfxEnabled()
        {
            try
            {
                var framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance();
                var configBase = framework->SystemConfig.CommonSystemConfig.ConfigBase;

                var seEnabled = false;
                var masterEnabled = false;

                for (var i = 0; i < configBase.ConfigCount; i++)
                {
                    var entry = configBase.ConfigEntry[i];

                    if (entry.Name != null)
                    {
                        var name = Dalamud.Memory.MemoryHelper.ReadStringNullTerminated(new IntPtr(entry.Name));

                        if (name == "IsSndSe")
                        {
                            var value = entry.Value.UInt;
                            PluginLog.Verbose($"[{Name}]: {name} - {entry.Type} - {value}");

                            seEnabled = value == 0;
                        }

                        if (name == "IsSndMaster")
                        {
                            var value = entry.Value.UInt;
                            PluginLog.Verbose($"[{Name}]: {name} - {entry.Type} - {value}");

                            masterEnabled = value == 0;
                        }
                    }
                }

                return seEnabled && masterEnabled;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"[{Name}]: Error checking if sfx is enabled");
                return true;
            }
        }

        private void SendChatAlert(string size)
        {
            var message = new SeStringBuilder()
                .AddUiForeground($"[{P.Name}] ", 45)
                .AddUiForeground($"[{Name}] ", 62)
                .AddText($"You hook a fish with a ")
                .AddUiForeground(size, 576)
                .AddText(" bite.")
                .Build();
            Svc.Chat.Print(message);
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Framework.Update += RunFeature;
            TugType = new SeTugType(Svc.SigScanner);
            audioHandler = new(System.IO.Path.Combine(Pi.AssemblyLocation.Directory?.FullName!, "Sounds"));
            base.Enable();
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool _) =>
        {
            ImGui.Checkbox("Light Tugs", ref Config.LightTugs);
            if (Config.LightTugs)
            {
                ImGui.Indent();
                ImGui.Checkbox("Chat Alerts##Light", ref Config.LightChat);
                ImGui.Checkbox("Sound Effect##Light", ref Config.PlayLightSound);
                ImGui.Unindent();
            }

            ImGui.Checkbox("Strong Tugs", ref Config.StrongTugs);
            if (Config.StrongTugs)
            {
                ImGui.Indent();
                ImGui.Checkbox("Chat Alerts##Strong", ref Config.StrongChat);
                ImGui.Checkbox("Sound Effect##Strong", ref Config.PlayStrongSound);
                ImGui.Unindent();
            }

            ImGui.Checkbox("Legendary Tugs", ref Config.LegendaryTugs);
            if (Config.LegendaryTugs)
            {
                ImGui.Indent();
                ImGui.Checkbox("Chat Alerts##Legendary", ref Config.LegendaryChat);
                ImGui.Checkbox("Sound Effect##Legendary", ref Config.PlayLegendarySound);
                ImGui.Unindent();
            }
        };
    }

    public class SeAddressBase
    {
        public readonly IntPtr Address;

        public SeAddressBase(Dalamud.Game.SigScanner sigScanner, string signature, int offset = 0)
        {
            Address = sigScanner.GetStaticAddressFromSig(signature);
            if (Address != IntPtr.Zero)
                Address += offset;
            var baseOffset = (ulong)Address.ToInt64() - (ulong)sigScanner.Module.BaseAddress.ToInt64();
        }
    }

    public sealed class SeTugType : SeAddressBase
    {
        public SeTugType(SigScanner sigScanner)
            : base(sigScanner,
                "4C 8D 0D ?? ?? ?? ?? 4D 8B 13 49 8B CB 45 0F B7 43 ?? 49 8B 93 ?? ?? ?? ?? 88 44 24 20 41 FF 92 ?? ?? ?? ?? 48 83 C4 38 C3")
        { }

        public unsafe FishNotify.BiteType Bite
            => Address != IntPtr.Zero ? *(FishNotify.BiteType*)Address : FishNotify.BiteType.Unknown;
    }

    public sealed class EventFramework : SeAddressBase
    {
        private const int FishingManagerOffset = 0x70;
        private const int FishingStateOffset = 0x220;

        internal unsafe IntPtr FishingManager
        {
            get
            {
                if (Address == IntPtr.Zero)
                    return IntPtr.Zero;

                var managerPtr = *(IntPtr*)Address + FishingManagerOffset;
                if (managerPtr == IntPtr.Zero)
                    return IntPtr.Zero;

                return *(IntPtr*)managerPtr;
            }
        }

        internal IntPtr FishingStatePtr
        {
            get
            {
                var ptr = FishingManager;
                if (ptr == IntPtr.Zero)
                    return IntPtr.Zero;

                return ptr + FishingStateOffset;
            }
        }

        public unsafe FishNotify.FishingState FishingState
        {
            get
            {
                var ptr = FishingStatePtr;
                return ptr != IntPtr.Zero ? *(FishNotify.FishingState*)ptr : FishNotify.FishingState.None;
            }
        }

        public EventFramework(Dalamud.Game.SigScanner sigScanner)
            : base(sigScanner,
                "48 8B 2D ?? ?? ?? ?? 48 8B F1 48 8B 85 ?? ?? ?? ?? 48 8B 18 48 3B D8 74 35 0F 1F 00 F6 83 ?? ?? ?? ?? ?? 75 1D 48 8B 46 28 48 8D 4E 28 48 8B 93 ?? ?? ?? ??")
        { }
    }
}
