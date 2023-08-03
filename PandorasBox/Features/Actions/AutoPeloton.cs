using Dalamud.Configuration;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Logging;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using System.Linq;

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
        }

        public Configs Config { get; private set; }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        private void RunFeature(Framework framework)
        {
            if (Svc.ClientState.LocalPlayer == null) return;

            if (IsRpWalking() && !Config.RPWalk) return;
            if (Svc.Condition[ConditionFlag.InCombat]) return;
            if (Svc.ClientState.LocalPlayer is null) return;
            if (Svc.Data.GetExcelSheet<TerritoryType>().First(x => x.RowId == Svc.ClientState.TerritoryType).Bg.RawString.Contains("/hou/") && Config.ExcludeHousing) return;

            var am = ActionManager.Instance();
            var isPeletonReady = am->GetActionStatus(ActionType.Spell, 7557) == 0;
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
            if (Config.OnlyInDuty && !Svc.Condition[ConditionFlag.BoundByDuty56]) return;

            var am = ActionManager.Instance();
            var isPeletonReady = am->GetActionStatus(ActionType.Spell, 7557) == 0;
            var hasPeletonBuff = Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 1199 || x.StatusId == 50);

            if (isPeletonReady && !hasPeletonBuff && AgentMap.Instance()->IsPlayerMoving == 1)
            {
                am->UseAction(ActionType.Spell, 7557);
            }
        }


        public override void Disable()
        {
            Svc.Framework.Update -= RunFeature;
            SaveConfig(Config);
            base.Disable();
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool _) =>
        {
            ImGui.PushItemWidth(300);
            ImGui.SliderFloat("Set Delay (seconds)", ref Config.ThrottleF, 0.1f, 10f, "%.1f");
            ImGui.Checkbox("Function only in a duty", ref Config.OnlyInDuty);
            ImGui.Checkbox("Use whilst walk status is toggled", ref Config.RPWalk);
            ImGui.Checkbox("Exclude Housing Zones", ref Config.ExcludeHousing);
        };
    }
}
