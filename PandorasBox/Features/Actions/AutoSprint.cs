using Dalamud.Game;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using PandorasBox.FeaturesSetup;
using System;
using System.ComponentModel.Design;
using System.Linq;

namespace PandorasBox.Features
{
    public unsafe class AutoSprint : Feature
    {
        public override string Name => "Auto-sprint in Sanctuaries";

        public override string Description => "Automatically uses sprint when in an area you are gaining rested experience, such as cities.";

        public override FeatureType FeatureType => FeatureType.Actions;

        public override bool UseAutoConfig => true;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Set delay (ms)", IntMin = 100, IntMax = 10000, EditorSize = 350)]
            public int Throttle = 100;

            [FeatureConfigOption("Use whilst walk status is toggled")]
            public bool RPWalk = false;
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
            if (!TerritoryInfo.Instance()->IsInSanctuary() || MJIManager.Instance()->IsPlayerInSanctuary == 1)
                return;

            if (IsRpWalking() && !Config.RPWalk)
                return;

            ActionManager* am = ActionManager.Instance();
            bool isSprintReady = am->GetActionStatus(ActionType.General, 4) == 0;
            bool? hasSprintBuff = Svc.ClientState.LocalPlayer?.StatusList.Any(x => x.StatusId == 50);

            if (isSprintReady && AgentMap.Instance()->IsPlayerMoving == 1 && !P.TaskManager.IsBusy)
            {
                P.TaskManager.Enqueue(() => EzThrottler.Throttle("Sprinting", Config.Throttle));
                P.TaskManager.Enqueue(() => EzThrottler.Check("Sprinting"));
                P.TaskManager.Enqueue(UseSprint);
            }
        }

        private void UseSprint()
        {
            ActionManager* am = ActionManager.Instance();
            bool isSprintReady = am->GetActionStatus(ActionType.General, 4) == 0;
            bool? hasSprintBuff = Svc.ClientState.LocalPlayer?.StatusList.Any(x => x.StatusId == 50);

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
    }
}
