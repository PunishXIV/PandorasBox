using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using System.Linq;
using System.Text.RegularExpressions;

namespace PandorasBox.Features
{
    public unsafe class AutoPeloton : Feature
    {
        public override string Name => "Auto-Peloton";
        public override string Description => "Uses Peloton automatically outside of combat. (Physical Ranged only)";
        public override FeatureType FeatureType => FeatureType.Actions;

        public Configs Config { get; private set; }
        public override bool UseAutoConfig => true;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Set delay (seconds)", "", 1, FloatMin = 0.1f, FloatMax = 10f, EditorSize = 300)]
            public float ThrottleF = 0.1f;

            [FeatureConfigOption("Function only in a duty", "", 2)]
            public bool OnlyInDuty = false;

            [FeatureConfigOption("Use whilst walk status is toggled", "", 3)]
            public bool RPWalk = false;

            [FeatureConfigOption("Exclude using in housing districts", "", 4)]
            public bool ExcludeHousing = false;
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        public override void Disable()
        {
            Svc.Framework.Update -= RunFeature;
            SaveConfig(Config);
            base.Disable();
        }

        private void RunFeature(IFramework framework)
        {
            if (Svc.ClientState.LocalPlayer == null) return;

            if (IsRpWalking() && !Config.RPWalk) return;
            if (Svc.Condition[ConditionFlag.InCombat]) return;
            if (Svc.ClientState.LocalPlayer is null) return;
            var r = new Regex("/hou/|/ind/");
            if (r.IsMatch(Svc.Data.GetExcelSheet<TerritoryType>().GetRow(Svc.ClientState.TerritoryType).Bg.RawString) && Config.ExcludeHousing) return;

            var am = ActionManager.Instance();
            var isPeletonReady = am->GetActionStatus(ActionType.Action, 7557) == 0;
            var hasPeletonBuff = Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 1199 || x.StatusId == 50);

            if (isPeletonReady && !hasPeletonBuff && AgentMap.Instance()->IsPlayerMoving == 1 && !TaskManager.IsBusy)
            {
                TaskManager.Enqueue(() => EzThrottler.Throttle("Pelotoning", (int)(Config.ThrottleF * 1000)));
                TaskManager.Enqueue(() => EzThrottler.Check("Pelotoning"));
                TaskManager.Enqueue(UsePeloton);
            }
        }

        private void UsePeloton()
        {
            if (IsRpWalking() && !Config.RPWalk) return;
            if (Svc.Condition[ConditionFlag.InCombat]) return;
            if (Svc.ClientState.LocalPlayer is null) return;
            if (Config.OnlyInDuty && !IsInDuty()) return;

            var am = ActionManager.Instance();
            var isPeletonReady = am->GetActionStatus(ActionType.Action, 7557) == 0;
            var hasPeletonBuff = Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 1199 || x.StatusId == 50);

            if (isPeletonReady && !hasPeletonBuff && AgentMap.Instance()->IsPlayerMoving == 1)
            {
                am->UseAction(ActionType.Action, 7557);
            }
        }
    }
}
