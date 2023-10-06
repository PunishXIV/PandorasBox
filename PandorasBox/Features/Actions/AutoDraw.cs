using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.JobGauge.Types;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using PandorasBox.FeaturesSetup;

namespace PandorasBox.Features.Actions
{
    public unsafe class AutoDraw : Feature
    {
        public override string Name => "Auto-Draw Card";

        public override string Description => "Draws an AST card upon job switching or entering a duty.";

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Set delay (seconds)", FloatMin = 0.1f, FloatMax = 10f, EditorSize = 300)]
            public float ThrottleF = 0.1f;

            [FeatureConfigOption("Function only in a duty")]
            public bool OnlyInDuty = false;
        }

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => true;

        public override FeatureType FeatureType => FeatureType.Actions;

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            OnJobChanged += DrawCard;
            Svc.ClientState.TerritoryChanged += CheckIfDungeon;
            Svc.Condition.ConditionChange += CheckIfRespawned;
            base.Enable();
        }

        private void CheckIfDungeon(ushort e)
        {
            if (GameMain.Instance()->CurrentContentFinderConditionId == 0) return;

            TaskManager.DelayNext("WaitForConditions", 2000);
            TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.BetweenAreas], "CheckConditionBetweenAreas");
            TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent], "CheckConditionCutscene");
            TaskManager.Enqueue(() => DrawCard(Svc.ClientState.LocalPlayer?.ClassJob.Id));
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
                TaskManager.Enqueue(() => DrawCard(Svc.ClientState.LocalPlayer?.ClassJob.Id));
            }
        }

        private void DrawCard(uint? jobId)
        {
            if (jobId == 33)
            {
                if (Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.UsingParasol]) return;

                TaskManager.DelayNext("ASTCard", (int)(Config.ThrottleF * 1000));
                TaskManager.Enqueue(() => TryDrawCard());
            }
        }

        private bool? TryDrawCard()
        {
            if (Config.OnlyInDuty && !Svc.Condition[ConditionFlag.BoundByDuty56]) return true;

            if (Svc.Gauges.Get<ASTGauge>().DrawnCard == Dalamud.Game.ClientState.JobGauge.Enums.CardType.NONE)
            {
                var am = ActionManager.Instance();
                if (am->GetActionStatus(ActionType.Action, 3590) != 0) return false;
                am->UseAction(ActionType.Action, 3590, Svc.ClientState.LocalPlayer.ObjectId);
                return true;
            }
            return false;
        }

        public override void Disable()
        {
            SaveConfig(Config);
            OnJobChanged -= DrawCard;
            Svc.ClientState.TerritoryChanged -= CheckIfDungeon;
            Svc.Condition.ConditionChange -= CheckIfRespawned;
            base.Disable();
        }
    }
}
