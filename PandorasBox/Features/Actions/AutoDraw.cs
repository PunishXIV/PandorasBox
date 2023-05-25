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
        }

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => true;

        public override FeatureType FeatureType => FeatureType.Actions;

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            OnJobChanged += DrawCard;
            Svc.ClientState.TerritoryChanged += CheckIfDungeon;
            base.Enable();
        }

        private void CheckIfDungeon(object sender, ushort e)
        {
            if (GameMain.Instance()->CurrentContentFinderConditionId == 0) return;

            TaskManager.DelayNext("WaitForConditions",2000);
            TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.BetweenAreas], "CheckConditionBetweenAreas");
            TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent], "CheckConditionCutscene");
            TaskManager.Enqueue(() => DrawCard(Svc.ClientState.LocalPlayer?.ClassJob.Id));
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
            if (Svc.Gauges.Get<ASTGauge>().DrawnCard == Dalamud.Game.ClientState.JobGauge.Enums.CardType.NONE)
            {
                var am = ActionManager.Instance();
                if (am->GetActionStatus(ActionType.Spell, 3590) != 0) return false;
                am->UseAction(ActionType.Spell, 3590, Svc.ClientState.LocalPlayer.ObjectId);
                return true;
            }
            return false;
        }

        public override void Disable()
        {
            SaveConfig(Config);
            OnJobChanged -= DrawCard;
            Svc.ClientState.TerritoryChanged -= CheckIfDungeon;
            base.Disable();
        }
    }
}
