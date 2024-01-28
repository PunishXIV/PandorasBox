using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.JobGauge.Types;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using PandorasBox.FeaturesSetup;
using System;

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

            [FeatureConfigOption("Trigger at Duty Start")]
            public bool DutyStart = false;

            [FeatureConfigOption("Trigger on Respawn")]
            public bool OnRespawn = false;

            [FeatureConfigOption("Trigger on Job Change")]
            public bool OnJobChange = true;

            [FeatureConfigOption("Trigger out of combat")]
            public bool OutOfCombat = false;
        }

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => true;

        public override FeatureType FeatureType => FeatureType.Actions;

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Events.OnJobChanged += DrawCard;
            Svc.ClientState.TerritoryChanged += CheckIfDungeon;
            Svc.Condition.ConditionChange += CheckIfRespawned;
            Svc.Framework.Update += CheckOutOfCombat;
            base.Enable();
        }

        private void CheckOutOfCombat(IFramework framework)
        {
            if (Config.OutOfCombat && !Svc.Condition[ConditionFlag.InCombat] && !TaskManager.IsBusy)
            {
                if (Svc.ClientState.LocalPlayer is null) return;
                if (Svc.ClientState.LocalPlayer.ClassJob.Id != 33) return;
                var am = ActionManager.Instance();
                if (am->GetActionStatus(ActionType.Action, 3590) != 0) return;
                if (Svc.Gauges.Get<ASTGauge>().DrawnCard != Dalamud.Game.ClientState.JobGauge.Enums.CardType.NONE) return;

                TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.BetweenAreas]);
                TaskManager.Enqueue(() => ActionManager.Instance()->GetActionStatus(ActionType.Action, 7) == 0);
                TaskManager.DelayNext("WaitForConditions", (int)(Config.ThrottleF * 1000));
                TaskManager.Enqueue(() => TryDrawCard());
            }
        }

        private void CheckIfDungeon(ushort e)
        {
            if (!Config.DutyStart) return;

            if (GameMain.Instance()->CurrentContentFinderConditionId == 0) return;

            TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.BetweenAreas]);
            TaskManager.Enqueue(() => ActionManager.Instance()->GetActionStatus(ActionType.Action, 7) == 0);
            TaskManager.DelayNext("WaitForConditions", (int)(Config.ThrottleF * 1000));
            TaskManager.Enqueue(() => TryDrawCard());
        }

        private void CheckIfRespawned(ConditionFlag flag, bool value)
        {
            if (!Config.OnRespawn) return;

            if (flag == ConditionFlag.Unconscious && !value && !Svc.Condition[ConditionFlag.InCombat])
            {
                TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.Unconscious], "CheckConditionUnconscious");
                TaskManager.Enqueue(() => !Svc.Condition[ConditionFlag.BetweenAreas], "CheckConditionBetweenAreas");
                TaskManager.Enqueue(() => ActionManager.Instance()->GetActionStatus(ActionType.Action, 7) == 0);
                TaskManager.DelayNext("WaitForActionReady", 2500);
                TaskManager.DelayNext("WaitForConditions", (int)(Config.ThrottleF * 1000));
                TaskManager.Enqueue(() => TryDrawCard());
            }
        }

        private void DrawCard(uint? jobId)
        {
            if (!Config.OnJobChange) return;

            if (jobId == 33)
            {
                if (Svc.Gauges.Get<ASTGauge>().DrawnCard != Dalamud.Game.ClientState.JobGauge.Enums.CardType.NONE) return;

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
                if (am->GetActionStatus(ActionType.Action, 3590) != 0) return false;
                am->UseAction(ActionType.Action, 3590, Svc.ClientState.LocalPlayer.ObjectId);
                return true;
            }
            return false;
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Events.OnJobChanged -= DrawCard;
            Svc.ClientState.TerritoryChanged -= CheckIfDungeon;
            Svc.Condition.ConditionChange -= CheckIfRespawned;
            Svc.Framework.Update -= CheckOutOfCombat;
            base.Disable();
        }
    }
}
