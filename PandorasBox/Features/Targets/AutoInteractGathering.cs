using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text.SeStringHandling;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using System;
using System.Linq;
using System.Numerics;

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

            [FeatureConfigOption("Exclude Timed Unspoiled Nodes", "", 1)]
            public bool ExcludeTimedUnspoiled = false;

            [FeatureConfigOption("Exclude Timed Ephemeral Nodes", "", 2)]
            public bool ExcludeTimedEphermeral = false;

            [FeatureConfigOption("Exclude Timed Legendary Nodes", "", 3)]
            public bool ExcludeTimedLegendary = false;

            [FeatureConfigOption("Required GP to Interact (>=)", IntMin = 0, IntMax = 1000, EditorSize = 300)]
            public int RequiredGP = 0;

            [FeatureConfigOption("Exclude Island Nodes", "", 7)]
            public bool ExcludeIsland = false;

            [FeatureConfigOption("Exclude Miner Nodes", "", 4)]
            public bool ExcludeMiner = false;

            [FeatureConfigOption("Exclude Botanist Nodes", "", 5)]
            public bool ExcludeBotanist = false;

            [FeatureConfigOption("Exclude Spearfishing Nodes", "", 6)]
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

        private void RunFeature(IFramework framework)
        {
            if (Svc.Condition[ConditionFlag.Gathering] || Svc.Condition[ConditionFlag.OccupiedInQuestEvent])
                return;

            if (Svc.ClientState.LocalPlayer is null) return;
            if (Svc.ClientState.LocalPlayer.IsCasting) return;
            if (Svc.Condition[ConditionFlag.Jumping]) return;

            var nearbyNodes = Svc.Objects.Where(x => (x.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.GatheringPoint || x.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.CardStand) && Vector3.Distance(x.Position, Player.Object.Position) < 3 && GameObjectHelper.GetHeightDifference(x) <= 3 && x.IsTargetable).ToList();
            if (nearbyNodes.Count == 0)
                return;

            var nearestNode = nearbyNodes.OrderBy(GameObjectHelper.GetTargetDistance).First();
            var baseObj = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)nearestNode.Address;

            if (!nearestNode.IsTargetable)
                return;

            if (Config.ExcludeIsland && MJIManager.Instance()->IsPlayerInSanctuary != 0)
            {
                return;
            }

            if (nearestNode.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.CardStand && MJIManager.Instance()->IsPlayerInSanctuary != 0 && MJIManager.Instance()->CurrentMode == 1)
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
            var targetGp = Math.Min(Config.RequiredGP, Svc.ClientState.LocalPlayer.MaxGp);

            string Folklore = "";

            if (gatheringPoint.GatheringSubCategory.IsValueCreated && gatheringPoint.GatheringSubCategory.Value.FolkloreBook != null)
                Folklore = gatheringPoint.GatheringSubCategory.Value.FolkloreBook.RawString;

            if (Svc.Data.GetExcelSheet<GatheringPointTransient>().Any(x => x.RowId == nearestNode.DataId && x.GatheringRarePopTimeTable.Value.RowId > 0 && gatheringPoint.GatheringSubCategory.Value?.Item.Row == 0) && Config.ExcludeTimedUnspoiled)
                return;
            if (Svc.Data.GetExcelSheet<GatheringPointTransient>().Any(x => x.RowId == nearestNode.DataId && x.EphemeralStartTime != 65535) && Config.ExcludeTimedEphermeral)
                return;
            if (Svc.Data.GetExcelSheet<GatheringPointTransient>().Any(x => x.RowId == nearestNode.DataId && x.GatheringRarePopTimeTable.Value.RowId > 0 && Folklore.Length > 0 && gatheringPoint.GatheringSubCategory.Value?.Item.Row != 0) && Config.ExcludeTimedLegendary)
                return;

            if (!Config.ExcludeMiner && job is 0 or 1 && Svc.ClientState.LocalPlayer.ClassJob.Id == 16 && Svc.ClientState.LocalPlayer.CurrentGp >= targetGp && !TaskManager.IsBusy)
            {
                TaskManager.DelayNext("Gathering", (int)(Config.Throttle * 1000));
                TaskManager.Enqueue(() => { Chat.Instance.SendMessage("/automove off"); });
                TaskManager.Enqueue(() => { TargetSystem.Instance()->OpenObjectInteraction(baseObj); return true; }, 1000);
                return;
            }
            if (!Config.ExcludeBotanist && job is 2 or 3 && Svc.ClientState.LocalPlayer.ClassJob.Id == 17 && Svc.ClientState.LocalPlayer.CurrentGp >= targetGp && !TaskManager.IsBusy)
            {
                TaskManager.DelayNext("Gathering", (int)(Config.Throttle * 1000));
                TaskManager.Enqueue(() => { Chat.Instance.SendMessage("/automove off"); });
                TaskManager.Enqueue(() => { TargetSystem.Instance()->OpenObjectInteraction(baseObj); return true; }, 1000);
                return;
            }
            if (!Config.ExcludeFishing && job is 4 or 5 && Svc.ClientState.LocalPlayer.ClassJob.Id == 18 && Svc.ClientState.LocalPlayer.CurrentGp >= targetGp && !TaskManager.IsBusy)
            {
                TaskManager.DelayNext("Gathering", (int)(Config.Throttle * 1000));
                TaskManager.Enqueue(() => { Chat.Instance.SendMessage("/automove off"); });
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
