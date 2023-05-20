using ECommons.DalamudServices;
using ECommons.SimpleGui;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using PandorasBox.FeaturesSetup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandorasBox.Features.Actions
{
    public unsafe class AutoFairy : Feature
    {
        public override string Name => "Auto-Summon Fairy/Carbuncle";

        public override string Description => "Automatically summons your Fairy or Carbuncle upon switching to SCH or SMN respectively.";

        public override FeatureType FeatureType => FeatureType.Actions;

        public class Configs : FeatureConfig
        {
            public float ThrottleF = 0.1f;

            public uint SelectedFairy = 0;
        }

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => false;

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            OnJobChanged += RunFeature;
            base.Enable();
        }

        private void RunFeature(uint? jobId)
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas]) return;
            if (jobId is 26 or 27 or 28)
            {
                TaskManager.Abort();
                TaskManager.DelayNext("Summoning", (int)(Config.ThrottleF * 1000));
                TaskManager.Enqueue(() => TrySummon(jobId), 5000);
            }
        }

        public bool TrySummon(uint? jobId)
        {
            var am = ActionManager.Instance();
            if (jobId is 26 or 27)
            {
                if (am->GetActionStatus(ActionType.Spell, 25798) != 0) return false;

                am->UseAction(ActionType.Spell, 25798);
            }
            if (jobId is 28)
            {
                if (am->GetActionStatus(ActionType.Spell, 17215) != 0) return false;
                switch (Config.SelectedFairy)
                {
                    case 0:
                        am->UseAction(ActionType.Spell, 17215);
                        return true;
                    case 1:
                        am->UseAction(ActionType.Spell, 17216);
                        return true;
                }

            }
            return true;
        }
        public override void Disable()
        {
            SaveConfig(Config);
            OnJobChanged -= RunFeature;
            base.Disable();
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool _) =>
        {
            ImGui.PushItemWidth(350);
            ImGui.SliderFloat("Set delay (seconds)", ref Config.ThrottleF, 0.1f, 10f, "%.1f");
            
            if (ImGui.RadioButton("Summon EoS", Config.SelectedFairy == 0))
            {
                Config.SelectedFairy = 0;
            }
            if (ImGui.RadioButton("Summon Selene", Config.SelectedFairy == 1))
            {
                Config.SelectedFairy = 1;
            }

        };
    }
}
