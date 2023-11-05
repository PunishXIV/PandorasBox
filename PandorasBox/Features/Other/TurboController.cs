using Dalamud.Game.ClientState.GamePad;
using Dalamud.Hooking;
using ECommons.DalamudServices;
using ECommons.Gamepad;
using ImGuiNET;
using PandorasBox.FeaturesSetup;
using System;
using System.Collections.Generic;
using System.Windows.Forms.Design;

namespace PandorasBox.Features.Other
{
    internal class TurboController : Feature
    {
        public override string Name { get; } = "Turbo Controller";
        public override string Description { get; } = "Enable rapid fire button presses on controller by holding down a button.";

        public override FeatureType FeatureType => FeatureType.Other;

        private Hook<ControllerPoll>? gamepadPoll;
        private delegate int ControllerPoll(IntPtr controllerInput);

        private long ThrottleTime { get; set; } = Environment.TickCount64;

        public class Configs : FeatureConfig
        {
            public int Throttle = 250;

            public List<GamepadButtons> ExcludedButtons = new();
        }

        public Configs Config { get; set; }

        public override bool UseAutoConfig => false;
        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            gamepadPoll ??= Svc.Hook.HookFromSignature<ControllerPoll>("40 ?? 57 41 ?? 48 81 EC ?? ?? ?? ?? 44 0F ?? ?? ?? ?? ?? ?? ?? 48 8B", GamepadPollDetour);
            gamepadPoll?.Enable();
            base.Enable();
        }

        private unsafe int GamepadPollDetour(IntPtr gamepadInput)
        {
            var input = (GamepadInput*)gamepadInput;

            foreach (var btn in GamePad.ControllerButtons)
            {
                if (GamePad.IsButtonHeld(btn.Key))
                {
                    if (Config.ExcludedButtons.Contains(btn.Key))
                        continue;

                    if (Environment.TickCount64 >= ThrottleTime)
                    {
                        ThrottleTime = Environment.TickCount64 + Config.Throttle;
                        input->ButtonsRaw -= (ushort)btn.Key;
                    }
                }
            }

            return gamepadPoll.Original((IntPtr)input);
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
        {
            if (ImGui.InputInt("Interval (ms)", ref Config.Throttle, 10, 50))
                hasChanged = true;

            ImGui.Spacing();
            ImGui.Text("Excluded Buttons");
            ImGui.Columns(4);
            foreach (var btn in GamePad.ControllerButtons)
            {
                if (btn.Key == GamepadButtons.None) continue;

                bool excluded = Config.ExcludedButtons.Contains(btn.Key);
                if (ImGui.Checkbox($"{btn.Value}", ref excluded))
                {
                    if (excluded)
                        Config.ExcludedButtons.Add(btn.Key);
                    else
                        Config.ExcludedButtons.RemoveAll(x => x == btn.Key);

                    hasChanged = true;
                }

                ImGui.NextColumn();
            }

            ImGui.Columns(1);

        };

        public override void Disable()
        {
            SaveConfig(Config);
            gamepadPoll?.Disable();
            base.Disable();
        }

        public override void Dispose()
        {
            gamepadPoll?.Dispose();
            base.Dispose();
        }
    }
}
