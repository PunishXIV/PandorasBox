using Dalamud.Game;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using PandorasBox.FeaturesSetup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandorasBox.Features
{
    public unsafe class SanctuarySprint : Feature
    {
        public override string Name => "Auto-sprint on Island Sanctuary";

        public override string Description => "Automatically uses Isle Sprint.";

        public override FeatureType FeatureType => FeatureType.Actions;

        public override void Enable()
        {
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        public override void Disable()
        {
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }

        private void RunFeature(Framework framework)
        {
            if (MJIManager.Instance()->IsPlayerInSanctuary == 0)
                return;

            var am = ActionManager.Instance();
            var isSprintReady = am->GetActionStatus(ActionType.Spell, 31314) == 0;
            var hasBuff = Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == 50 && x.RemainingTime >= 1f);

            if (isSprintReady && !hasBuff && AgentMap.Instance()->IsPlayerMoving == 1)
                am->UseAction(ActionType.Spell, 31314);

        }
    }
}
