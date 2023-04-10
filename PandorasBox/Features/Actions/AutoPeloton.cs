using Dalamud.Game;
using ECommons.DalamudServices;
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

        public override void Enable()
        {
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        private void RunFeature(Framework framework)
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat]) return;
            if (Svc.ClientState.LocalPlayer is null) return;

            ActionManager* am = ActionManager.Instance();
            bool isPeletonReady = am->GetActionStatus(ActionType.Spell, 7557) == 0;
            bool hasPeletonBuff = Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 1199 || x.StatusId == 50);


            if (isPeletonReady && !hasPeletonBuff && AgentMap.Instance()->IsPlayerMoving == 1)
            {
                am->UseAction(ActionType.Spell, 7557, Svc.ClientState.LocalPlayer.ObjectId);
            }
        }

        public override void Disable()
        {
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }
    }
}
