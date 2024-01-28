using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using System.Linq;
using System.Text.RegularExpressions;

namespace PandorasBox.Features
{
    public unsafe class AutoPeloton : Feature
    {
        public override string Name => "Auto-Peloton";

        public override string Description => "Uses Peloton automatically outside of combat. (Physical Ranged only)";

        public override FeatureType FeatureType => FeatureType.Actions;

        public override bool UseAutoConfig => false;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Set delay (seconds)", FloatMin = 0.1f, FloatMax = 10f, EditorSize = 300)]
            public float ThrottleF = 0.1f;

            [FeatureConfigOption("Function only in a duty")]
            public bool OnlyInDuty = false;

            [FeatureConfigOption("Use whilst walk status is toggled")]
            public bool RPWalk = false;

            [FeatureConfigOption("Exclude using in housing districts")]
            public bool ExcludeHousing = false;

            [FeatureConfigOption("Abort pending Peloton use during countdown")]
            public bool AbortCooldown = false;
        }

        public Configs Config { get; private set; }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        private void RunFeature(IFramework framework)
        {
            if (Svc.ClientState.LocalPlayer == null) return;

            if (IsRpWalking() && !Config.RPWalk) return;
            if (Svc.Condition[ConditionFlag.InCombat]) return;
            if (Svc.ClientState.LocalPlayer is null) return;
            var r = new Regex("/hou/|/ind/");
            if (r.IsMatch(Svc.Data.GetExcelSheet<TerritoryType>().GetRow(Svc.ClientState.TerritoryType).Bg.RawString) && Config.ExcludeHousing) return;

            if (Config.AbortCooldown && Countdown.TimeRemaining() > 0)
            {
                TaskManager.Abort();
                return;
            }

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
            if (Config.OnlyInDuty && GameMain.Instance()->CurrentContentFinderConditionId == 0) return;

            var am = ActionManager.Instance();
            var isPeletonReady = am->GetActionStatus(ActionType.Action, 7557) == 0;
            var hasPeletonBuff = Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 1199 || x.StatusId == 50);

            if (isPeletonReady && !hasPeletonBuff && AgentMap.Instance()->IsPlayerMoving == 1)
            {
                am->UseAction(ActionType.Action, 7557);
            }
        }


        public override void Disable()
        {
            Svc.Framework.Update -= RunFeature;
            SaveConfig(Config);
            base.Disable();
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
        {
            ImGui.PushItemWidth(300);
            if (ImGui.SliderFloat("Set Delay (seconds)", ref Config.ThrottleF, 0.1f, 10f, "%.1f")) hasChanged = true;
            if (ImGui.Checkbox("Function only in a duty", ref Config.OnlyInDuty)) hasChanged = true;
            if (ImGui.Checkbox("Use whilst walk status is toggled", ref Config.RPWalk)) hasChanged = true;
            if (ImGui.Checkbox("Exclude Housing Zones", ref Config.ExcludeHousing)) hasChanged = true;
            if (ImGui.Checkbox($"Abort pending Peloton use during countdown", ref Config.AbortCooldown)) hasChanged = true;
        };
    }
}
