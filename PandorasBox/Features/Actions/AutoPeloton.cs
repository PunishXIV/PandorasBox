using Dalamud.Game;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using PandorasBox.FeaturesSetup;
using System.Linq;

namespace PandorasBox.Features
{
    public unsafe class AutoPeloton : Feature
    {
        public override string Name => "Auto-Peloton";

        public override string Description => "Uses Peloton automatically outside of combat. (Physical Ranged only)";

        public override FeatureType FeatureType => FeatureType.Actions;

        public override bool UseAutoConfig => true;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Set delay (seconds)", FloatMin = 0.1f, FloatMax = 10f, EditorSize = 300)]
            public float ThrottleF = 100;

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
            if (IsRpWalking() && !Config.RPWalk) return;
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat]) return;
            if (Svc.ClientState.LocalPlayer is null) return;

            ActionManager* am = ActionManager.Instance();
            bool isPeletonReady = am->GetActionStatus(ActionType.Spell, 7557) == 0;
            bool hasPeletonBuff = Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 1199 || x.StatusId == 50);

            if (isPeletonReady && !hasPeletonBuff && AgentMap.Instance()->IsPlayerMoving == 1 && !TaskManager.IsBusy)
            {
                TaskManager.Enqueue(() => EzThrottler.Throttle("Pelotoning", (int)(Config.ThrottleF * 1000)));
                TaskManager.Enqueue(() => EzThrottler.Check("Pelotoning"));
                TaskManager.Enqueue(UsePeloton);
            }
        }

        private void UsePeloton()
        {
            ActionManager* am = ActionManager.Instance();
            bool isPeletonReady = am->GetActionStatus(ActionType.Spell, 7557) == 0;
            bool hasPeletonBuff = Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 1199 || x.StatusId == 50);

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
    }
}
