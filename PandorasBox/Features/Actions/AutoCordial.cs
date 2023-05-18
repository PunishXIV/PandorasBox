using PandorasBox.Features;
using PandorasBox.FeaturesSetup;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using System.Linq;
using ECommons.Throttlers;
using ImGuiNET;
using System.Collections.Generic;

namespace PandorasBox.Features.Actions
{
    public unsafe class AutoCordial : Feature
    {
        public override string Name => "Auto-Cordial";

        public override string Description => "Automatically use a cordial when below the threshold.";

        public override FeatureType FeatureType => FeatureType.Actions;

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => false;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("GP Threshold", IntMin = 0, IntMax = 1000, EditorSize = 300)]
            public int DefaultThreshold = 1;
            public bool DirectionAbove = true;
            public bool DirectionBelow = false;

            [FeatureConfigOption("Invert Priority (Watered -> Regular -> Hi)")]
            public bool InvertPriority = false;

            [FeatureConfigOption("Prevent Overcap")]
            public bool PreventOvercap = true;

            [FeatureConfigOption("Use on Fisher")]
            public bool UseOnFisher = false;
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool _) =>
        {
            ImGui.PushItemWidth(300);
            ImGui.SliderInt("GP Threshold", ref Config.DefaultThreshold, 1, 1000);
            if (ImGui.RadioButton("Above", Config.DirectionAbove))
            {
                Config.DirectionAbove = true;
                Config.DirectionBelow = false;
            }
            ImGui.SameLine();
            if (ImGui.RadioButton("Below", Config.DirectionBelow))
            {
                Config.DirectionAbove = false;
                Config.DirectionBelow = true;
            }
            ImGui.Checkbox("Invert Priority (Watered -> Regular -> Hi)", ref Config.InvertPriority);
            ImGui.Checkbox("Prevent Overcap", ref Config.PreventOvercap);
            ImGui.Checkbox("Use on Fisher", ref Config.UseOnFisher);
        };

        private bool WillOvercap(int gp_recovery)
        {
            return Svc.ClientState.LocalPlayer.CurrentGp + gp_recovery < Svc.ClientState.LocalPlayer.MaxGp;
        }

        private void RunFeature(Framework framework)
        {
            if (Svc.Condition[ConditionFlag.InCombat] || Svc.ClientState.LocalPlayer.ClassJob.Id != 16 || Svc.ClientState.LocalPlayer.ClassJob.Id != 17) return;
            if (Svc.ClientState.LocalPlayer.ClassJob.Id == 18 && !Config.UseOnFisher) return;
            if (!((Svc.ClientState.LocalPlayer.CurrentGp < Config.DefaultThreshold && Config.DirectionBelow) || (Svc.ClientState.LocalPlayer.CurrentGp > Config.DefaultThreshold && Config.DirectionAbove))) return;

            ActionManager* am = ActionManager.Instance();

            uint hi_cordial = 12669;
            uint reg_cordial = 6141;
            uint reg_cordial_hq = 1006141;
            uint watered_cordial = 16911;
            uint watered_cordial_hq = 1016911;
            List<uint> cordials = new List<uint>() { hi_cordial, reg_cordial_hq, reg_cordial, watered_cordial_hq, watered_cordial };

            int hi_recovery = 400;
            int reg_recovery_hq = 350;
            int reg_recovery = 300;
            int water_recovery_hq = 200;
            int water_recovery = 150;
            List<int> recoveries = new List<int>() { hi_recovery, reg_recovery_hq, reg_recovery, water_recovery_hq, water_recovery };


            if (!Config.InvertPriority)
            {
                for (int i = 0; i < cordials.Count; i++)
                {
                    if (am->GetActionStatus(ActionType.Item, cordials[i]) == 0)
                    {
                        if (Config.PreventOvercap && !WillOvercap(recoveries[i]) || !Config.PreventOvercap)
                        {
                            am->UseAction(ActionType.Item, cordials[i], a4: 65535);
                        }
                    }
                }
            }
            else
            {
                for (int i = cordials.Count - 1; i >= 0; i--)
                {
                    if (am->GetActionStatus(ActionType.Item, cordials[i]) == 0)
                    {
                        if (Config.PreventOvercap && !WillOvercap(recoveries[i]) || !Config.PreventOvercap)
                        {
                            am->UseAction(ActionType.Item, cordials[i], a4: 65535);
                        }
                    }
                }
            }
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        public override void Disable()
        {
            Svc.Framework.Update -= RunFeature;
            SaveConfig(Config);
            base.Disable();
        }
    }
}
