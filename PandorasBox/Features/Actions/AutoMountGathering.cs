using Dalamud.Logging;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using PandorasBox.FeaturesSetup;
using System;

namespace PandorasBox.Features.Actions
{
    public unsafe class AutoMountGathering : Feature
    {
        public override string Name => "Auto-Mount after Gathering";

        public override string Description => "Uses Mount Roulette upon finishing gathering from a node.";

        public override FeatureType FeatureType => FeatureType.Actions;

        public override void Enable()
        {
            Svc.Condition.ConditionChange += RunFeature;
            base.Enable();
        }

        private void RunFeature(Dalamud.Game.ClientState.Conditions.ConditionFlag flag, bool value)
        {
            if (flag == Dalamud.Game.ClientState.Conditions.ConditionFlag.Gathering && !value)
            {
                P.TaskManager.Enqueue(TryMount, 3000);
            }
        }

        private bool? TryMount()
        {
            ActionManager* am = ActionManager.Instance();
            if (am->GetActionStatus(ActionType.General, 9) != 0) return false;

            am->UseAction(ActionType.General, 9);

            return true;
        }

        public override void Disable()
        {
            Svc.Condition.ConditionChange -= RunFeature;
            base.Disable();
        }
    }
}
