using Dalamud.Game;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using System.Linq;

namespace PandorasBox.Features.Targets
{
    public unsafe class AutoOpenChests : Feature
    {
        public override string Name => "Automatically Open Chests";

        public override string Description => "Walk up to a chest to automatically open it.";

        public override FeatureType FeatureType => FeatureType.Targeting;

        private const float slowCheckInterval = 0.3f;
        private float slowCheckRemaining = 0.0f;

        public override void Enable()
        {
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        private void RunFeature(Framework framework)
        {
            slowCheckRemaining -= (float)Svc.Framework.UpdateDelta.Milliseconds / 1000;

            if (slowCheckRemaining <= 0.0f)
            {
                slowCheckRemaining = slowCheckInterval;

                if (P.TaskManager.NumQueuedTasks > 0)
                    return;

                var nearbyNodes = Svc.Objects.Where(x => x.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Treasure && GameObjectHelper.GetTargetDistance(x) < 2).ToList();
                if (nearbyNodes.Count == 0)
                    return;

                var nearestNode = nearbyNodes.OrderBy(GameObjectHelper.GetTargetDistance).First();
                var baseObj = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)nearestNode.Address;

                if (!baseObj->GetIsTargetable())
                    return;

                P.TaskManager.Enqueue(() => { TargetSystem.Instance()->InteractWithObject(baseObj, true); return true; });

            }
        }

        public override void Disable()
        {
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }
    }
}
