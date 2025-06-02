using PandorasBox.FeaturesSetup;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Dalamud.Game.ClientState.Conditions;

namespace PandorasBox.Features.Actions
{
    public unsafe class AutoChocobo : Feature
    {
        public override string Name => "Auto-Summon Chocobo";

        public override string Description => "Automatically consumes a Gysahl Green in the overworld if you don't have your chocobo out.";

        public override FeatureType FeatureType => FeatureType.Actions;

        public Configs Config { get; private set; } = null!;

        public override bool UseAutoConfig => true;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Set time left to resummon (seconds)", IntMin = 0, IntMax = 600, EditorSize = 300)]
            public int RemainingTimeLimit = 0;

            [FeatureConfigOption("Use whilst in a party")]
            public bool UseInParty = true;

            [FeatureConfigOption("Use whilst in combat")]
            public bool UseInCombat = false;

            [FeatureConfigOption("Prevent Use After 5 Minutes Idle")]
            public bool AfkCheck = true;
        }

        private void RunFeature(IFramework framework)
        {
            if (!Svc.Condition[ConditionFlag.NormalConditions] || Svc.Condition[ConditionFlag.Casting] || IsMoving()) return;
            if (Svc.Condition[ConditionFlag.InCombat] && !Config.UseInCombat) return;
            if (Svc.Party.Length > 1 && !Config.UseInParty) return;
            if (IsAFK() && Config.AfkCheck) return;

            var am = ActionManager.Instance();
            if (UIState.Instance()->Buddy.CompanionInfo.TimeLeft <= Config.RemainingTimeLimit)
            {
                if (am->GetActionStatus(ActionType.Item, 4868) != 0) return;
                am->UseAction(ActionType.Item, 4868, extraParam: 65535);
            }
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            UseAFKTimer = true;
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
