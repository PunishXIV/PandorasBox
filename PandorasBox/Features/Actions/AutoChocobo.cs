using PandorasBox.Features;
using PandorasBox.FeaturesSetup;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using System.Linq;

namespace PandorasBox.Features.Actions
{
    public unsafe class AutoChocobo : Feature
    {
        public override string Name => "Auto Summon Chocobo";

        public override string Description => "Automatically consumes a gysahl green in the overworld if you don't have your bird out.";

        public override FeatureType FeatureType => FeatureType.Actions;

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => true;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Set time left to resummon (seconds)", IntMin = 0, IntMax = 600, EditorSize = 300)]
            public int RemainingTimeLimit = 0;

            [FeatureConfigOption("Use whilst in a party")]
            public bool UseInParty = true;

            [FeatureConfigOption("Use whilst in combat")]
            public bool UseInCombat = false;
        }

        private void RunFeature(Framework framework)
        {
            if (!Svc.Condition[ConditionFlag.NormalConditions] || Svc.Condition[ConditionFlag.Casting] || IsMoving()) return;
            if (Svc.Condition[ConditionFlag.InCombat] && !Config.UseInCombat) return;
            if (Svc.Party.Count() > 1 && !Config.UseInParty) return;

            ActionManager* am = ActionManager.Instance();
            if (UIState.Instance()->Buddy.TimeLeft <= Config.RemainingTimeLimit)
            {
                if (am->GetActionStatus(ActionType.Item, 4868) != 0) return;
                am->UseAction(ActionType.Item, 4868, a4: 65535);
            }
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
    }
}
