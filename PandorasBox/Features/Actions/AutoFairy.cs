using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using PandorasBox.FeaturesSetup;

namespace PandorasBox.Features.Actions
{
    public unsafe class AutoFairy : Feature
    {
        public override string Name => "Auto-Summon Fairy/Carbuncle";
        public override string Description => "Automatically summons your Fairy or Carbuncle upon switching to SCH or SMN respectively.";
        public override FeatureType FeatureType => FeatureType.Actions;

        public Configs Config { get; private set; }
        public override bool UseAutoConfig => true;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Set delay (seconds)", FloatMin = 0.1f, FloatMax = 10f, EditorSize = 300)]
            public float ThrottleF = 0.1f;

            [FeatureConfigOption("Function only in a duty")]
            public bool OnlyInDuty = false;
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            OnJobChanged += RunFeature;
            Svc.Condition.ConditionChange += CheckIfRespawned;
            base.Enable();
        }

        public override void Disable()
        {
            SaveConfig(Config);
            OnJobChanged -= RunFeature;
            Svc.Condition.ConditionChange -= CheckIfRespawned;
            base.Disable();
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
                TaskManager.Enqueue(() => ActionManager.Instance()->GetActionStatus(ActionType.Action, 7) == 0);
                TaskManager.DelayNext("WaitForActionReady", 2500);
                TaskManager.DelayNext("WaitForConditions", (int)(Config.ThrottleF * 1000));
                TaskManager.Enqueue(() => TrySummon(Svc.ClientState.LocalPlayer?.ClassJob.Id), 5000);
            }
        }

        public bool TrySummon(uint? jobId)
        {
            if (Config.OnlyInDuty && !IsInDuty()) return true;

            var am = ActionManager.Instance();
            if (jobId is 26 or 27 or 28)
            {
                var actionID = jobId == 28 ? 17215u : 25798u;
                if (am->GetActionStatus(ActionType.Action, actionID) != 0) return false;

                am->UseAction(ActionType.Action, actionID);
            }
            return true;
        }
    }
}
