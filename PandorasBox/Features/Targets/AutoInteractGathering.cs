using Dalamud.Logging;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using System.Linq;

namespace PandorasBox.Features.Targets
{
    public unsafe class AutoInteractGathering : Feature
    {
        public override string Name => "Auto-interact with Gathering Nodes";
        public override string Description => "Interacts with trees and rocks when close enough and on the correct job.";

        public override FeatureType FeatureType => FeatureType.Targeting;

        private const float slowCheckInterval = 0.1f;
        private float slowCheckRemaining = 0.0f;

        public override bool UseAutoConfig => true;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Set delay (ms)", IntMin = 100, IntMax = 10000, EditorSize = 350)]
            public int Throttle =  100;

            [FeatureConfigOption("Exclude Timed Nodes", "", 1)]
            public bool ExcludeTimed = false;
        }

        public Configs Config { get; private set; }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        private void RunFeature(Dalamud.Game.Framework framework)
        {
            slowCheckRemaining -= (float)Svc.Framework.UpdateDelta.Milliseconds / 1000;

            if (slowCheckRemaining <= 0.0f)
            {
                slowCheckRemaining = slowCheckInterval;

                if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Gathering])
                    return;

                var nearbyNodes = Svc.Objects.Where(x => x.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.GatheringPoint && GameObjectHelper.GetTargetDistance(x) < 2).ToList();
                if (nearbyNodes.Count == 0)
                    return;

                var nearestNode = nearbyNodes.OrderBy(GameObjectHelper.GetTargetDistance).First();
                var baseObj = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)nearestNode.Address;

                if (!baseObj->GetIsTargetable())
                    return;

                var gatheringPoint = Svc.Data.GetExcelSheet<GatheringPoint>().First(x => x.RowId == nearestNode.DataId);
                var job = gatheringPoint.GatheringPointBase.Value.GatheringType.Value.RowId;

                if (Svc.Data.GetExcelSheet<GatheringPointTransient>().Any(x => x.RowId == nearestNode.DataId && (x.GatheringRarePopTimeTable.Value.RowId > 0 || x.EphemeralStartTime != 65535)) && Config.ExcludeTimed)
                    return;

                if (job is 0 or 1 && Svc.ClientState.LocalPlayer.ClassJob.Id == 16 && !TaskManager.IsBusy)
                {
                    Svc.Targets.Target = nearestNode;
                    TaskManager.DelayNext("Gathering", Config.Throttle);
                    TaskManager.Enqueue(() => { TargetSystem.Instance()->InteractWithObject(baseObj); return true; }, 1000);
                    return;
                }
                if (job is 2 or 3 && Svc.ClientState.LocalPlayer.ClassJob.Id == 17 && !TaskManager.IsBusy)
                {
                    Svc.Targets.Target = nearestNode;
                    TaskManager.DelayNext("Gathering", Config.Throttle);
                    TaskManager.Enqueue(() => { TargetSystem.Instance()->InteractWithObject(baseObj); return true; }, 1000);
                    return;
                }
            }
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }
    }
}
