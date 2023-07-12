using Dalamud.Game;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Text.RegularExpressions;

namespace PandorasBox.Features
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

            if (!TerritoryInfo.Instance()->IsInSanctuary() || MJIManager.Instance()->IsPlayerInSanctuary == 1)
                return;

            var r = new Regex("/hou/|/ind/");
            if (r.IsMatch(Svc.Data.GetExcelSheet<TerritoryType>().First(x => x.RowId == Svc.ClientState.TerritoryType).Bg.RawString) && Config.ExcludeHousing) return;

            if (IsRpWalking() && !Config.RPWalk)
                return;

            var am = ActionManager.Instance();
            var isSprintReady = am->GetActionStatus(ActionType.General, 4) == 0;
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
            var isSprintReady = am->GetActionStatus(ActionType.General, 4) == 0;
            var hasSprintBuff = Svc.ClientState.LocalPlayer?.StatusList.Any(x => x.StatusId == 50);

            if (isSprintReady && AgentMap.Instance()->IsPlayerMoving == 1)
            {
                am->UseAction(ActionType.General, 4);
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
            ImGui.Checkbox("Use whilst walk status is toggled", ref Config.RPWalk);
            ImGui.Checkbox("Exclude Housing Zones", ref Config.ExcludeHousing);

        };
    }
}
