using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;

namespace PandorasBox.Features.Other
{
    public unsafe class AutoRecommended : Feature
    {
        public override string Name => "Auto-Equip Recommended Gear";

        public override string Description => "Automatically equip recommended gear upon job changing.";

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Update Gearset")]
            public bool UpdateGearset = false;
        }

        public Configs? Config { get; private set; }
        public override FeatureType FeatureType => FeatureType.Other;

        public override bool UseAutoConfig => true;

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Events.OnJobChanged += AutoEquip;
            base.Enable();
        }

        private void AutoEquip(uint? jobId)
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat]) return;
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas]) return;
            var mod = RecommendEquipModule.Instance();
            //TaskManager.Abort();
            TaskManager!.EnqueueDelay(500);
            TaskManager.EnqueueWithTimeout(() => mod->SetupForClassJob((byte)Svc.ClientState.LocalPlayer!.ClassJob.RowId), 500);
            TaskManager.EnqueueWithTimeout(() => mod->EquipRecommendedGear(), 500);

            if (Config!.UpdateGearset)
            {
                var id = RaptureGearsetModule.Instance()->CurrentGearsetIndex;
                TaskManager.EnqueueDelay(1000);
                TaskManager.Enqueue(() => RaptureGearsetModule.Instance()->UpdateGearset(id));
            }
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Events.OnJobChanged -= AutoEquip;
            base.Disable();
        }
    }
}
