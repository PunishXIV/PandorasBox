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
            [FeatureConfigOption("Set delay (ms)", IntMin = 100, IntMax = 10000, EditorSize = 350)]
            public int Throttle = 100;

            [FeatureConfigOption("Include Truth of Mountains/Forests", "", 1)]
            public bool AddTruth = false;
        }

        public Configs Config { get; private set; }

        private uint? jobID;
        public uint? JobID
        {
            get => jobID;
            set
            {
                if (jobID != value)
                {
                    if (value is 16 or 17)
                    {
                        TaskManager.DelayNext("ProspectTriangulate", Config.Throttle);
                        TaskManager.Enqueue(() => ActivateBuff(value));
                    }
                }
                jobID = value;
            }
        }

        private bool? ActivateBuff(uint? value)
        {
            ActionManager* am = ActionManager.Instance();   
            if (Svc.ClientState.LocalPlayer.StatusList.Where(x => x.StatusId == 217 || x.StatusId == 225).Count() == 2)
                return true;

            if (value == 16 && am->GetActionStatus(ActionType.Spell, 210) == 0)
            {
                am->UseAction(ActionType.Spell, 210);
                if (Config.AddTruth && am->GetActionStatus(ActionType.Spell, 221) == 0)
                {
                   TaskManager.EnqueueImmediate(() => am->UseAction(ActionType.Spell, 221));
                }
                return true;
            }
            if (value == 17 && am->GetActionStatus(ActionType.Spell, 227) == 0)
            {
                am->UseAction(ActionType.Spell, 227);
                if (Config.AddTruth && am->GetActionStatus(ActionType.Spell, 238) == 0)
                {
                    TaskManager.EnqueueImmediate(() => am->UseAction(ActionType.Spell, 238));
                }
                return true;
            }

            return false;

        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        private void RunFeature(Framework framework)
        {
            if (Svc.ClientState.LocalPlayer is null) return;
            JobID = Svc.ClientState.LocalPlayer.ClassJob.Id;
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }
    }
}
