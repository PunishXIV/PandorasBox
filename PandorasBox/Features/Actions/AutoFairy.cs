using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;

namespace PandorasBox.Features.Actions
{
    public unsafe class AutoFairy : Feature
    {
        public override string Name => "Auto-Summon Fairy/Carbuncle";

        public override string Description => "Automatically summons your Fairy or Carbuncle upon switching to SCH or SMN respectively.";

        public override FeatureType FeatureType => FeatureType.Actions;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Set delay (seconds)", FloatMin = 0.1f, FloatMax = 10f, EditorSize = 300)]
            public float ThrottleF = 0.1f;

            [FeatureConfigOption("Trigger at Duty Start")]
            public bool DutyStart = false;

            [FeatureConfigOption("Trigger on Respawn")]
            public bool OnRespawn = false;

            [FeatureConfigOption("Trigger on Job Change")]
            public bool OnJobChange = true;
        }

        public Configs Config { get; private set; } = null!;

        public override bool UseAutoConfig => true;

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Events.OnJobChanged += RunFeature;
            Svc.ClientState.TerritoryChanged += CheckForDuty;
            Svc.Condition.ConditionChange += CheckIfRespawned;
            base.Enable();
        }

        private void CheckForDuty(ushort obj)
        {
            if (!Config.DutyStart) return;

            if (GameMain.Instance()->CurrentContentFinderConditionId == 0) return;

            TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.BetweenAreas]);
            TaskManager.Enqueue(() => ActionManager.Instance()->GetActionStatus(ActionType.Action, 7) == 0);
            TaskManager.EnqueueDelay((int)(Config.ThrottleF * 1000));
            TaskManager.EnqueueWithTimeout(() => TrySummon(Svc.ClientState.LocalPlayer?.ClassJob.RowId), 5000);
        }

        private void RunFeature(uint? jobId)
        {
            if (!Config.OnJobChange) return;

            if (Svc.Condition[ConditionFlag.BetweenAreas]) return;
            if (jobId is 26 or 27 or 28)
            {
                TaskManager.EnqueueDelay((int)(Config.ThrottleF * 1000));
                TaskManager.EnqueueWithTimeout(() => TrySummon(jobId), 5000);
            }
        }

        private void CheckIfRespawned(ConditionFlag flag, bool value)
        {
            if (!Config.OnRespawn) return;

            if (flag == ConditionFlag.Unconscious && !value && !Svc.Condition[ConditionFlag.InCombat])
            {
                TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Unconscious], "CheckConditionUnconscious");
                TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.BetweenAreas], "CheckConditionBetweenAreas");
                TaskManager.Enqueue(() => ActionManager.Instance()->GetActionStatus(ActionType.Action, 7) == 0);
                TaskManager.EnqueueDelay(2500);
                TaskManager.EnqueueDelay((int)(Config.ThrottleF * 1000));
                TaskManager.EnqueueWithTimeout(() => TrySummon(Svc.ClientState.LocalPlayer?.ClassJob.RowId), 5000);
            }
        }

        public bool TrySummon(uint? jobId)
        {
            if (Svc.Buddies.PetBuddy != null) return true;

            var am = ActionManager.Instance();
            if (jobId is 26 or 27)
            {
                if (am->GetActionStatus(ActionType.Action, 25798) != 0) return false;

                am->UseAction(ActionType.Action, 25798);
            }
            if (jobId is 28)
            {
                if (am->GetActionStatus(ActionType.Action, 17215) != 0) return false;

                am->UseAction(ActionType.Action, 17215);
                return true;
            }
            return true;
        }
        public override void Disable()
        {
            SaveConfig(Config);
            Events.OnJobChanged -= RunFeature;
            Svc.Condition.ConditionChange -= CheckIfRespawned;
            base.Disable();
        }
    }
}
