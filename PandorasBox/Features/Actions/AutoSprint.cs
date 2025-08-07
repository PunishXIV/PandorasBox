using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using PandorasBox.FeaturesSetup;
using System.Linq;
using System.Text.RegularExpressions;

namespace PandorasBox.Features.Actions
{
    public unsafe class AutoSprint : Feature
    {
        public override string Name => "Auto-Sprint in Sanctuaries";

        public override string Description => "Automatically uses sprint when in an area you are gaining rested experience, such as cities.";

        public override FeatureType FeatureType => FeatureType.Actions;

        public override bool UseAutoConfig => false;

        public class Configs : FeatureConfig
        {
            public float ThrottleF = 0.1f;
            public bool RPWalk = false;
            public bool ExcludeHousing = false;
        }

        public Configs Config { get; private set; } = null!;

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        private void RunFeature(IFramework framework)
        {
            if (Svc.ClientState.LocalPlayer == null) return;

            if (!TerritoryInfo.Instance()->InSanctuary || MJIManager.Instance()->IsPlayerInSanctuary)
                return;

            var r = new Regex("/hou/|/ind/");
            var loc = Svc.Data.GetExcelSheet<TerritoryType>().GetRow(Svc.ClientState.TerritoryType).Bg.ToString();
            if (r.IsMatch(loc) && Config.ExcludeHousing) return;

            if (IsRpWalking() && !Config.RPWalk)
                return;

            var am = ActionManager.Instance();
            var isSprintReady = am->GetActionStatus(ActionType.GeneralAction, 4) == 0;
            var hasSprintBuff = Svc.ClientState.LocalPlayer?.StatusList.Any(x => x.StatusId == 50);

            if (isSprintReady && IsMoving() && !TaskManager.IsBusy)
            {
                TaskManager.Enqueue(() => EzThrottler.Throttle("Sprinting", (int)(Config.ThrottleF * 1000)));
                TaskManager.Enqueue(() => EzThrottler.Check("Sprinting"));
                TaskManager.Enqueue(UseSprint);
            }
        }

        private void UseSprint()
        {
            var am = ActionManager.Instance();
            var isSprintReady = am->GetActionStatus(ActionType.GeneralAction, 4) == 0;
            var hasSprintBuff = Svc.ClientState.LocalPlayer?.StatusList.Any(x => x.StatusId == 50);

            if (isSprintReady && IsMoving())
            {
                am->UseAction(ActionType.GeneralAction, 4);
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
            if (ImGui.Checkbox("Use whilst walk status is toggled", ref Config.RPWalk)) hasChanged = true;
            if (ImGui.Checkbox("Exclude Housing Zones", ref Config.ExcludeHousing)) hasChanged = true;
        };
    }
}
