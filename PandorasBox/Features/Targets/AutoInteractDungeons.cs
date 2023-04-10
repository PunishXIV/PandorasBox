using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandorasBox.Features.Targets
{
    public unsafe class AutoInteractDungeons : Feature
    {
        public override string Name => "Auto-interact with Objects in Instances";

        public override string Description => "Automatically try to pick all the keys, levers, and other thingymabobs. Also works to try and open doors and stuff.";

        public override FeatureType FeatureType => FeatureType.Targeting;

        private const float slowCheckInterval = 0.1f;
        private float slowCheckRemaining = 0.0f;

        public override void Enable()
        {
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        private void RunFeature(Dalamud.Game.Framework framework)
        {
            slowCheckRemaining -= (float)Svc.Framework.UpdateDelta.Milliseconds / 1000;

            if (slowCheckRemaining <= 0.0f)
            {
                slowCheckRemaining = slowCheckInterval;

                if (Svc.ClientState.LocalPlayer == null) return;
                if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty])
                {
                    var nearbyNodes = Svc.Objects.Where(x => x.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventObj && GameObjectHelper.GetTargetDistance(x) < 2).ToList();
                    if (nearbyNodes.Count == 0)
                        return;

                    var nearestNode = nearbyNodes.OrderBy(GameObjectHelper.GetTargetDistance).First();
                    var baseObj = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)nearestNode.Address;

                    if (!baseObj->GetIsTargetable())
                        return;

                    Svc.Targets.Target = nearestNode;
                    P.TaskManager.Enqueue(() => { TargetSystem.Instance()->InteractWithObject(baseObj); return true; }, 1000);
                }
            }
        }

        public override void Disable()
        {
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }
    }
}
