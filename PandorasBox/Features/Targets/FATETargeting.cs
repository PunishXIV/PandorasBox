using ECommons.DalamudServices;
using ECommons.GameFunctions;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using PandorasBox.FeaturesSetup;
using System.Linq;
using ECommons;
using FFXIVClientStructs.FFXIV.Client.Game;

namespace PandorasBox.Features.Targets
{
    internal class FATETargeting : Feature
    {
        public override string Name { get; } = "FATE Targeting Mode";
        public override string Description { get; } = "When in a FATE and able to participate (synced), automatically targets FATE associated enemies.";
        public override FeatureType FeatureType { get; } = FeatureType.Targeting;

        public override void Enable()
        {
            Svc.Framework.Update += Framework_Update;
            base.Enable();
        }

        private unsafe void Framework_Update(IFramework framework)
        {
            var fate = FateManager.Instance();
            var am = ActionManager.Instance();
            if (fate != null && fate->CurrentFate != null && Svc.Objects.LocalPlayer?.Level < fate->FateDirector->FateLevel + 6)
            {
                var tar = Svc.Targets.Target;
                if (tar == null || tar.IsDead || (tar.Struct()->FateId == 0 && tar.IsHostile()))
                {
                    if (Svc.Objects.OrderBy(x => am->DistanceToTargetHitbox).TryGetFirst(x => x.Struct()->FateId == fate->CurrentFate->FateId && x.IsHostile(), out var newTar))
                    {
                        Svc.Targets.Target = newTar;
                    }
                }
            }
        }

        public override void Disable()
        {
            Svc.Framework.Update -= Framework_Update;
            base.Disable();
        }
    }
}
