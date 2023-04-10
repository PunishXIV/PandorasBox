using Dalamud.Logging;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;

namespace PandorasBox.Features.Actions
{
    public unsafe class AutoMountZoneChange : Feature
    {
        public override string Name => "Auto-mount on Zone Change";

        public override string Description => "Uses Mount Roulette on zone change if not already mounted.";

        public override FeatureType FeatureType => FeatureType.Actions;

        public override void Enable()
        {
            Svc.ClientState.TerritoryChanged += RunFeature;
            base.Enable();
        }

        private void RunFeature(object sender, ushort e)
        {
            P.TaskManager.Enqueue(TryMount);
        }

        private bool? TryMount()
        {
            if (Svc.ClientState.LocalPlayer is null) return false;
            ActionManager* am = ActionManager.Instance();
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas]) return false;
            if (am->GetActionStatus(ActionType.General, 9, Svc.ClientState.LocalPlayer.ObjectId) != 0) return null;
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Mounted]) return null;

            P.TaskManager.Enqueue(() => EzThrottler.Throttle("MountTeleport", 500));
            P.TaskManager.Enqueue(() => EzThrottler.Check("MountTeleport"));
            P.TaskManager.Enqueue(() => am->UseAction(ActionType.General, 9));

            return true;
        }

        public override void Disable()
        {
            Svc.ClientState.TerritoryChanged -= RunFeature;
            base.Disable();
        }
    }
}
