using Dalamud.Game;
using ECommons.DalamudServices;
using ECommons.SimpleGui;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using PandorasBox.FeaturesSetup;
using System;
using System.Linq;

namespace PandorasBox.Features.Actions
{
    public unsafe class AutoProspectTriangulate : Feature
    {
        public override string Name => "Auto-Prospect/Triangulate";

        public override string Description => "When switching to MIN or BTN, automatically activate the other jobs searching ability.";

        public override FeatureType FeatureType => FeatureType.Actions;

        public override bool UseAutoConfig => true;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Set delay (seconds)", FloatMin = 0.1f, FloatMax = 10f, EditorSize = 300)]
            public float ThrottleF = 0.1f;

            [FeatureConfigOption("Include Truth of Mountains/Forests", "", 1)]
            public bool AddTruth = false;
        }

        public Configs Config { get; private set; }

        
        private void ActivateBuff(uint? jobValue)
        {
            if (jobValue is null) return;
            TaskManager.DelayNext(this.GetType().Name, (int)(Config.ThrottleF * 1000));
            ActionManager* am = ActionManager.Instance();   
            if (Svc.ClientState.LocalPlayer?.StatusList.Where(x => x.StatusId == 217 || x.StatusId == 225).Count() == 2)
                return;
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Gathering])
            {
                TaskManager.Abort();
                return;
            }

            if (jobValue == 16 && am->GetActionStatus(ActionType.Spell, 210) == 0)
            {
                TaskManager.Enqueue(() => am->UseAction(ActionType.Spell, 210));
                if (Config.AddTruth && am->GetActionStatus(ActionType.Spell, 221) == 0)
                {
                   TaskManager.Enqueue(() => am->UseAction(ActionType.Spell, 221));
                }
                return;
            }
            if (jobValue == 17 && am->GetActionStatus(ActionType.Spell, 227) == 0)
            {
                TaskManager.Enqueue(() => am->UseAction(ActionType.Spell, 227));
                if (Config.AddTruth && am->GetActionStatus(ActionType.Spell, 238) == 0)
                {
                    TaskManager.Enqueue(() => am->UseAction(ActionType.Spell, 238));
                }
                return;
            }

        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            OnJobChanged += ActivateBuff;
            base.Enable();
        }

        public override void Disable()
        {
            SaveConfig(Config);
            base.Disable();
        }
    }
}
