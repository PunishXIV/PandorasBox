using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using ECommons;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using System;
using System.Linq;

namespace PandorasBox.Features.Targets
{
    public unsafe class AutoInteractGathering : Feature
    {
        public override string Name => "Auto-interact with Gathering Nodes";
        public override string Description => "Interacts with gathering nodes when close enough and on the correct job.";

        public override FeatureType FeatureType => FeatureType.Targeting;

        public override bool UseAutoConfig => true;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Set delay (seconds)", FloatMin = 0.1f, FloatMax = 10f, EditorSize = 300, EnforcedLimit = true)]
            public float Throttle = 0.1f;

            [FeatureConfigOption("Cooldown after gathering (seconds)", FloatMin = 0.1f, FloatMax = 10f, EditorSize = 300, EnforcedLimit = true)]
            public float Cooldown = 0.1f;

            [FeatureConfigOption("Required GP to Interact (>=)", IntMin = -1, IntMax = 1000, EditorSize = 300)]
            public int RequiredGP = -1;

            [FeatureConfigOption("Exclude Timed Nodes", "", 1)]
            public bool ExcludeTimed = false;

            [FeatureConfigOption("Exclude Island Nodes", "", 4)]
            public bool ExcludeIsland = false;

            [FeatureConfigOption("Exclude Miner Nodes", "", 2)]
            public bool ExcludeMiner = false;

            [FeatureConfigOption("Exclude Botanist Nodes", "", 1)]
            public bool ExcludeBotanist = false;

            [FeatureConfigOption("Exclude Spearfishing Nodes", "", 3)]
            public bool ExcludeFishing = false;
        }

        public Configs Config { get; private set; }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Framework.Update += RunFeature;
            Svc.Condition.ConditionChange += TriggerCooldown;
            Svc.Toasts.ErrorToast += CheckIfLanding;
            base.Enable();
        }

        private void CheckIfLanding(ref SeString message, ref bool isHandled)
        {
            if (message.ExtractText() == Svc.Data.GetExcelSheet<LogMessage>().First(x => x.RowId == 7777).Text.ExtractText())
            {
                TaskManager.Abort();
                TaskManager.DelayNext("ErrorMessage", 2000);
            }
        }

        private void TriggerCooldown(ConditionFlag flag, bool value)
        {
            if (flag == ConditionFlag.Gathering && !value)
                TaskManager.DelayNext("GatheringDelay", (int)(Config.Cooldown * 1000));
        }

        private void RunFeature(Dalamud.Game.Framework framework)
        {
            if (Svc.Condition[ConditionFlag.Gathering] || Svc.Condition[ConditionFlag.OccupiedInQuestEvent])
                return;

            if (Svc.ClientState.LocalPlayer is null) return;

            var nearbyNodes = Svc.Objects.Where(x => (x.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.GatheringPoint || x.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.CardStand) && GameObjectHelper.GetTargetDistance(x) < 2 && GameObjectHelper.GetHeightDifference(x) <= 3).ToList();
            if (nearbyNodes.Count == 0)
                return;

            var nearestNode = nearbyNodes.OrderBy(GameObjectHelper.GetTargetDistance).First();
            var baseObj = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)nearestNode.Address;

            if (!baseObj->GetIsTargetable())
                return;

            if (!Config.ExcludeIsland && nearestNode.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.CardStand && MJIManager.Instance()->IsPlayerInSanctuary != 0 && MJIManager.Instance()->CurrentMode == 1)
            {
                if (!TaskManager.IsBusy)
                {
                    TaskManager.DelayNext("Gathering", (int)(Config.Throttle * 1000));
                    TaskManager.Enqueue(() => { TargetSystem.Instance()->OpenObjectInteraction(baseObj); return true; }, 1000);
                }
                return;
            }

            var gatheringPoint = Svc.Data.GetExcelSheet<GatheringPoint>().First(x => x.RowId == nearestNode.DataId);
            var job = gatheringPoint.GatheringPointBase.Value.GatheringType.Value.RowId;
            var targetGp = Config.RequiredGP > 0 ? Math.Min(Config.RequiredGP, Svc.ClientState.LocalPlayer.MaxGp) : Svc.ClientState.LocalPlayer.MaxGp;

            if (Svc.Data.GetExcelSheet<GatheringPointTransient>().Any(x => x.RowId == nearestNode.DataId && (x.GatheringRarePopTimeTable.Value.RowId > 0 || x.EphemeralStartTime != 65535)) && Config.ExcludeTimed)
                return;

            if (!Config.ExcludeMiner && job is 0 or 1 && Svc.ClientState.LocalPlayer.ClassJob.Id == 16 && Svc.ClientState.LocalPlayer.CurrentGp >= targetGp && !TaskManager.IsBusy)
            {
                TaskManager.DelayNext("Gathering", (int)(Config.Throttle * 1000));
                TaskManager.Enqueue(() => { TargetSystem.Instance()->OpenObjectInteraction(baseObj); return true; }, 1000);
                return;
            }
            if (!Config.ExcludeBotanist && job is 2 or 3 && Svc.ClientState.LocalPlayer.ClassJob.Id == 17 && Svc.ClientState.LocalPlayer.CurrentGp >= targetGp && !TaskManager.IsBusy)
            {
                TaskManager.DelayNext("Gathering", (int)(Config.Throttle * 1000));
                TaskManager.Enqueue(() => { TargetSystem.Instance()->OpenObjectInteraction(baseObj); return true; }, 1000);
                return;
            }
            if (!Config.ExcludeFishing && job is 4 or 5 && Svc.ClientState.LocalPlayer.ClassJob.Id == 18 && Svc.ClientState.LocalPlayer.CurrentGp >= targetGp && !TaskManager.IsBusy)
            {
                TaskManager.DelayNext("Gathering", (int)(Config.Throttle * 1000));
                TaskManager.Enqueue(() => { TargetSystem.Instance()->OpenObjectInteraction(baseObj); return true; }, 1000);
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
