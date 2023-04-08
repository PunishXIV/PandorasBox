using Dalamud.Game;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandorasBox.Features
{
    public unsafe class AutoSprint : Feature
    {
        public override string Name => "Auto-sprint in Sanctuaries";

        public override string Description => "Automatically uses sprint when in an area you are gaining rested experience, such as cities.";

        public override FeatureType FeatureType => FeatureType.Actions;

        public class Configs : FeatureConfig
        {

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

            ActionManager* am = ActionManager.Instance();
            bool isSprintReady = am->GetActionStatus(ActionType.General, 4) == 0;

            if (isSprintReady && AgentMap.Instance()->IsPlayerMoving == 1)
                am->UseAction(ActionType.General, 4);
        }

        public override void Disable()
        {
            Svc.Framework.Update -= RunFeature;
            SaveConfig(Config);
            base.Disable();
        }
    }
}
