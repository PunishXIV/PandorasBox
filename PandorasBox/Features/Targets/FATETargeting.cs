using ECommons.DalamudServices;
using ECommons.GameFunctions;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using PandorasBox.FeaturesSetup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ECommons.GameHelpers;
using ECommons;

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
            if (fate != null && fate->CurrentFate != null && fate->SyncedFateId > 0)
            {
                var tar = Svc.Targets.Target;
                if (tar == null || (tar.Struct()->FateId == 0 && tar.IsHostile()))
                {
                    if (Svc.Objects.OrderBy(x => x.GetTargetDistance()).TryGetFirst(x => x.Struct()->FateId == fate->CurrentFate->FateId && x.IsHostile(), out var newTar))
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
