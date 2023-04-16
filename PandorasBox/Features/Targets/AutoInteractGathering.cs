using Dalamud.Game.ClientState.Conditions;
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

        public override bool UseAutoConfig => true;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Set delay (seconds)", FloatMin = 0.1f, FloatMax = 10f, EditorSize = 300, EnforcedLimit = true)]
            public float Throttle = 0.1f;

            [FeatureConfigOption("Cooldown after gathering (seconds)", FloatMin = 0.1f, FloatMax = 10f, EditorSize = 300, EnforcedLimit = true)]
            public float Cooldown = 0.1f;

            [FeatureConfigOption("Exclude Timed Nodes", "", 1)]
            public bool ExcludeTimed = false;
        }

        public Configs Config { get; private set; }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Framework.Update += RunFeature;
            Svc.Condition.ConditionChange += TriggerCooldown;
            base.Enable();
        }

        private void TriggerCooldown(ConditionFlag flag, bool value)
        {
            if (flag == ConditionFlag.Gathering && !value)
                TaskManager.DelayNext("GatheringDelay", (int)(Config.Cooldown * 1000));
        }

        private void RunFeature(Dalamud.Game.Framework framework)
        {

            if (Svc.Condition[ConditionFlag.Gathering])
                return;

            if (Svc.ClientState.LocalPlayer is null) return;

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
                TaskManager.DelayNext("Gathering", (int)(Config.Throttle * 1000));
                TaskManager.Enqueue(() => { TargetSystem.Instance()->InteractWithObject(baseObj); return true; }, 1000);
                return;
            }
            if (job is 2 or 3 && Svc.ClientState.LocalPlayer.ClassJob.Id == 17 && !TaskManager.IsBusy)
            {
                Svc.Targets.Target = nearestNode;
                TaskManager.DelayNext("Gathering", (int)(Config.Throttle * 1000));
                TaskManager.Enqueue(() => { TargetSystem.Instance()->InteractWithObject(baseObj); return true; }, 1000);
                return;
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
