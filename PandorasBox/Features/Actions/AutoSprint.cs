using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using System.Linq;
using System.Text.RegularExpressions;

namespace PandorasBox.Features
{
    public unsafe class AutoSprint : Feature
    {
        public override string Name => "Auto-Sprint in Sanctuaries";
        public override string Description => "Automatically uses sprint when in an area you are gaining rested experience, such as cities.";
        public override FeatureType FeatureType => FeatureType.Actions;

        public Configs Config { get; private set; }
        public override bool UseAutoConfig => true;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Set delay (seconds)", "", 1, FloatMin = 0.1f, FloatMax = 10f, EditorSize = 300)]
            public float ThrottleF = 0.1f;

            [FeatureConfigOption("Use whilst walk status is toggled", "", 2)]
            public bool RPWalk = false;

            [FeatureConfigOption("Exclude using in housing districts", "", 3)]
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

            if (!TerritoryInfo.Instance()->IsInSanctuary() || MJIManager.Instance()->IsPlayerInSanctuary == 1)
                return;

            var r = new Regex("/hou/|/ind/");
            var loc = Svc.Data.GetExcelSheet<TerritoryType>().GetRow(Svc.ClientState.TerritoryType).Bg.RawString;
            if (r.IsMatch(loc) && Config.ExcludeHousing) return;

            if (IsRpWalking() && !Config.RPWalk)
                return;

            var am = ActionManager.Instance();
            var isSprintReady = am->GetActionStatus(ActionType.GeneralAction, 4) == 0;
            var hasSprintBuff = Svc.ClientState.LocalPlayer?.StatusList.Any(x => x.StatusId == 50);

            if (isSprintReady && AgentMap.Instance()->IsPlayerMoving == 1 && !P.TaskManager.IsBusy)
            {
                P.TaskManager.Enqueue(() => EzThrottler.Throttle("Sprinting", (int)(Config.ThrottleF * 1000)));
                P.TaskManager.Enqueue(() => EzThrottler.Check("Sprinting"));
                P.TaskManager.Enqueue(UseSprint);
            }
        }

        private void UseSprint()
        {
            var am = ActionManager.Instance();
            var isSprintReady = am->GetActionStatus(ActionType.GeneralAction, 4) == 0;
            var hasSprintBuff = Svc.ClientState.LocalPlayer?.StatusList.Any(x => x.StatusId == 50);

            if (isSprintReady && AgentMap.Instance()->IsPlayerMoving == 1)
            {
                am->UseAction(ActionType.GeneralAction, 4);
            }
        }
    }
}
