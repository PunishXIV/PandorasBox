using Dalamud.Game.Text.SeStringHandling;
using ECommons.DalamudServices;
using PandorasBox.FeaturesSetup;
using System;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using Dalamud.Interface.Colors;
using ImGuiNET;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.Game.Event;
using System.Linq;

namespace PandorasBox.Features.Other
{
    /// <summary>
    /// Alerts when a fish bites while fishing using memory hooks for instant detection
    /// </summary>
    public unsafe class FishNotify : Feature
    {
        public override string Name => "Fish Notify";

        public override string Description => "Based on FishNotify by Liza Carvelli.";

        public class Configs : FeatureConfig
        {
            public bool EnableSound = true;
            public bool EnableChat = false;
            public bool DifferentSounds = true;
            public bool ShowDebug = false;
            

            public string LightSoundFile = "Light.wav";
            public string MediumSoundFile = "Strong.wav";
            public string HeavySoundFile = "Legendary.wav";
        }

        public override FeatureType FeatureType => FeatureType.Other;
        public override bool UseAutoConfig => false;
        
        public Configs Config { get; private set; }


        private IntPtr _tugTypeAddress;
        private BiteType _lastBiteType = BiteType.Unknown;
        private FishingState _lastFishingState = FishingState.NotFishing;
        private uint _fishCount = 0;
        private bool _memoryHookInitialized = false;


        private SoundPlayer? _lightSound;
        private SoundPlayer? _strongSound;
        private SoundPlayer? _legendarySound;
        private string[] _availableSounds = Array.Empty<string>();
        private string _soundsPath = string.Empty;
        



        private const int FishingManagerOffset = 0x70;

        public override void Setup()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            InitializeSounds();
            base.Setup();
        }

        public override void Enable()
        {
            if (Config == null)
                Config = LoadConfig<Configs>() ?? new Configs();
            
            InitializeMemoryHook();
            

            LoadSoundFiles();
            
            if (_memoryHookInitialized)
            {
                Svc.Framework.Update += OnFrameworkUpdate;
                Svc.Log.Debug($"[FishNotify] Enabled - Memory hook at {_tugTypeAddress:X}");
            }
            else
            {
                Svc.Log.Error("[FishNotify] Failed to initialize - memory hook not found");
            }
            

            Svc.Chat.ChatMessage += OnChatMessage;
            
            base.Enable();
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Svc.Framework.Update -= OnFrameworkUpdate;
            Svc.Chat.ChatMessage -= OnChatMessage;
            DisposeSounds();
            base.Disable();
        }

        private void InitializeMemoryHook()
        {
            try
            {
                // Signature: 48 8D 35 ?? ?? ?? ?? 4C 8B CE (bite/tug type location)
                _tugTypeAddress = Svc.SigScanner.GetStaticAddressFromSig("48 8D 35 ?? ?? ?? ?? 4C 8B CE");
                
                if (_tugTypeAddress != IntPtr.Zero)
                {
                    _memoryHookInitialized = true;
                    Svc.Log.Debug($"[FishNotify] Found tug type address at: {_tugTypeAddress:X}");
                }
                else
                {
                    Svc.Log.Error("[FishNotify] Failed to find tug type address!");
                }
            }
            catch (Exception e)
            {
                Svc.Log.Error($"[FishNotify] Failed to initialize memory hook: {e.Message}");
            }
        }

        private void InitializeSounds()
        {
            try
            {
                _soundsPath = Path.Combine(Svc.PluginInterface.AssemblyLocation.Directory?.FullName ?? "", "Sounds");
                
                Svc.Log.Debug($"[FishNotify] Looking for sounds in: {_soundsPath}");
                
                
                if (Directory.Exists(_soundsPath))
                {
                    _availableSounds = Directory.GetFiles(_soundsPath, "*.wav")
                        .Select(Path.GetFileName)
                        .Where(f => f != null)
                        .Cast<string>()
                        .ToArray();
                    
                    Svc.Log.Debug($"[FishNotify] Found {_availableSounds.Length} sound files: {string.Join(", ", _availableSounds)}");
                }
                else
                {
                    Svc.Log.Warning($"[FishNotify] Sounds directory does not exist: {_soundsPath}");
                    _availableSounds = Array.Empty<string>();
                }
                
                LoadSoundFiles();
            }
            catch (Exception e)
            {
                Svc.Log.Error($"[FishNotify] Failed to initialize sounds: {e.Message}");
                _availableSounds = Array.Empty<string>();
            }
        }
        
        private void LoadSoundFiles()
        {
            try
            {

                _lightSound?.Dispose();
                _strongSound?.Dispose();
                _legendarySound?.Dispose();
                

                var lightPath = Path.Combine(_soundsPath, Config!.LightSoundFile);
                var strongPath = Path.Combine(_soundsPath, Config.MediumSoundFile);
                var legendaryPath = Path.Combine(_soundsPath, Config.HeavySoundFile);

                if (File.Exists(lightPath))
                    _lightSound = new SoundPlayer(lightPath);
                
                if (File.Exists(strongPath))
                    _strongSound = new SoundPlayer(strongPath);
                
                if (File.Exists(legendaryPath))
                    _legendarySound = new SoundPlayer(legendaryPath);

                Svc.Log.Debug($"[FishNotify] Sounds loaded from {_soundsPath}");
                Svc.Log.Debug($"[FishNotify] Light={Config.LightSoundFile}, Medium={Config.MediumSoundFile}, Heavy={Config.HeavySoundFile}");
            }
            catch (Exception e)
            {
                Svc.Log.Error($"[FishNotify] Failed to load sound files: {e.Message}");
            }
        }

        private void DisposeSounds()
        {
            _lightSound?.Dispose();
            _strongSound?.Dispose();
            _legendarySound?.Dispose();
        }

        private FishingManagerStruct* GetFishingManager()
        {
            var managerPtr = (nint)EventFramework.Instance() + FishingManagerOffset;
            if (managerPtr == nint.Zero)
                return null;
            return *(FishingManagerStruct**)managerPtr;
        }

        private FishingState GetFishingState()
        {
            var ptr = GetFishingManager();
            return ptr != null ? ptr->FishingState : FishingState.NotFishing;
        }

        private void OnFrameworkUpdate(global::Dalamud.Plugin.Services.IFramework framework)
        {
            if (!Enabled || !_memoryHookInitialized)
                return;

            if (_tugTypeAddress == IntPtr.Zero)
                return;

            try
            {

                var currentFishingState = GetFishingState();
                

                if (_lastFishingState != FishingState.Bite && currentFishingState == FishingState.Bite)
                {
    
                    var biteType = *(BiteType*)_tugTypeAddress;
                    
                    if (Config!.ShowDebug)
                    {
                        Svc.Log.Info($"[FishNotify] Bite detected! State: {currentFishingState}, Type: {biteType} ({(byte)biteType})");
                        Svc.Chat.Print($"[FishNotify Debug] BITE! Type: {biteType} (value: {(byte)biteType})");
                    }
                    
                    OnFishBite(biteType);
                }
                
                _lastFishingState = currentFishingState;
            }
            catch (Exception e)
            {
                Svc.Log.Error($"[FishNotify] Error in update: {e.Message}");
            }
        }
        
        private void OnChatMessage(Dalamud.Game.Text.XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
        {

            if (type == Dalamud.Game.Text.XivChatType.Echo)
            {
                var text = message.TextValue.ToLower();
                if (text.Contains("test-fish-light"))
                {
                    Task.Run(() => _lightSound?.PlaySync());
                    Svc.Chat.Print("[FishNotify] Playing Light Tug sound");
                }
                else if (text.Contains("test-fish-medium"))
                {
                    Task.Run(() => _strongSound?.PlaySync());
                    Svc.Chat.Print("[FishNotify] Playing Medium Tug sound");
                }
                else if (text.Contains("test-fish-heavy"))
                {
                    Task.Run(() => _legendarySound?.PlaySync());
                    Svc.Chat.Print("[FishNotify] Playing Heavy Tug sound");
                }
            }
        }

        private void OnFishBite(BiteType biteType)
        {
            _fishCount++;
            
            var biteName = biteType switch
            {
                BiteType.Weak => "Light Tug",
                BiteType.Strong => "Medium Tug",
                BiteType.Legendary => "Heavy Tug",
                _ => $"Unknown ({(byte)biteType})"
            };
            

            Svc.Log.Info($"[FishNotify] Fish bite detected: {biteName} (#{_fishCount})");
            

            if (Config!.EnableChat)
            {
                var message = new SeStringBuilder()
                    .AddUiForeground($"[Fish Notify] ", 508)
                    .AddText($"{biteName} detected!")
                    .Build();
                
                Svc.Chat.Print(message);
            }
            

            if (Config!.EnableSound)
            {
                Svc.Log.Info($"[FishNotify] Playing sound for {biteName}");
                PlayBiteSound(biteType);
            }
        }

        private void PlayBiteSound(BiteType biteType)
        {
            try
            {
                SoundPlayer? soundToPlay = null;

                if (Config!.DifferentSounds)
                {
                    soundToPlay = biteType switch
                    {
                        BiteType.Weak => _lightSound,
                        BiteType.Strong => _strongSound,
                        BiteType.Legendary => _legendarySound,
                        _ => _strongSound
                    };
                }
                else
                {

                    soundToPlay = _strongSound;
                }


                if (soundToPlay != null)
                {
                    Task.Run(() => soundToPlay.PlaySync());
                }
            }
            catch (Exception e)
            {
                Svc.Log.Error($"[FishNotify] Failed to play sound: {e.Message}");
            }
        }
        

        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
        {

            if (ImGui.Checkbox("Enable Sound Notifications", ref Config.EnableSound))
                hasChanged = true;
                
            if (ImGui.Checkbox("Enable Chat Notifications", ref Config.EnableChat))
                hasChanged = true;
                
            if (ImGui.Checkbox("Play Different Sounds for Different Bite Types", ref Config.DifferentSounds))
                hasChanged = true;
                
            if (ImGui.Checkbox("Show Debug Information", ref Config.ShowDebug))
                hasChanged = true;
            

            ImGui.Separator();
            ImGui.Text("Test Sounds:");
            ImGui.SameLine();
            
            if (ImGui.Button("Light Tug (!)"))
            {
                Task.Run(() => _lightSound?.PlaySync());
                Svc.Chat.Print("[FishNotify] Playing Light Tug sound");
            }
            
            ImGui.SameLine();
            if (ImGui.Button("Medium Tug (!!)"))
            {
                Task.Run(() => _strongSound?.PlaySync());
                Svc.Chat.Print("[FishNotify] Playing Medium Tug sound");
            }
            
            ImGui.SameLine();
            if (ImGui.Button("Heavy Tug (!!!)"))
            {
                Task.Run(() => _legendarySound?.PlaySync());
                Svc.Chat.Print("[FishNotify] Playing Heavy Tug sound");
            }
        };


        private enum BiteType : byte
        {
            Unknown = 0,
            Weak = 36,      // ! 
            Strong = 37,    // !!
            Legendary = 38  // !!!
        }


        private enum FishingState : byte
        {
            NotFishing = 0,
            Bite = 5
        }


        [StructLayout(LayoutKind.Explicit)]
        private struct FishingManagerStruct
        {
            [FieldOffset(0x228)] public FishingState FishingState;
        }
    }
}
