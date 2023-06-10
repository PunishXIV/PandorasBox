using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using PandorasBox.FeaturesSetup;

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
        }

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => false;

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            OnJobChanged += RunFeature;
            Svc.Condition.ConditionChange += CheckIfRespawned;
            base.Enable();
        }

        private void RunFeature(uint? jobId)
        {
            if (Svc.Condition[ConditionFlag.BetweenAreas]) return;
            if (jobId is 26 or 27 or 28)
            {
                TaskManager.Abort();
                TaskManager.DelayNext("Summoning", (int)(Config.ThrottleF * 1000));
                TaskManager.Enqueue(() => TrySummon(jobId), 5000);
            }
        }

        private void CheckIfRespawned(ConditionFlag flag, bool value)
        {
            if (flag == ConditionFlag.Unconscious && !value && !Svc.Condition[ConditionFlag.InCombat])
            {
                TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Unconscious], "CheckConditionUnconscious");
                TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.BetweenAreas], "CheckConditionBetweenAreas");
                TaskManager.Enqueue(() => ActionManager.Instance()->GetActionStatus(ActionType.Spell, 7) == 0);
                TaskManager.DelayNext("WaitForActionReady", 2500);
                TaskManager.DelayNext("WaitForConditions", (int)(Config.ThrottleF * 1000));
                TaskManager.Enqueue(() => TrySummon(Svc.ClientState.LocalPlayer?.ClassJob.Id), 5000);
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

                am->UseAction(ActionType.Spell, 17215);
                return true;
            }
            return true;
        }
        public override void Disable()
        {
            SaveConfig(Config);
            OnJobChanged -= RunFeature;
            Svc.Condition.ConditionChange -= CheckIfRespawned;
            base.Disable();
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool _) =>
        {
            ImGui.PushItemWidth(350);
            ImGui.SliderFloat("Set delay (seconds)", ref Config.ThrottleF, 0.1f, 10f, "%.1f");

            //if (ImGui.RadioButton("Summon EoS", Config.SelectedFairy == 0))
            //{
            //    Config.SelectedFairy = 0;
            //}
            //if (ImGui.RadioButton("Summon Selene", Config.SelectedFairy == 1))
            //{
            //    Config.SelectedFairy = 1;
            //}

        };
    }
}
