using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.JobGauge.Types;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.EzHookManager;
using ECommons.GameHelpers;
using PandorasBox.FeaturesSetup;

namespace PandorasBox.Features.Actions
{
    internal class AutoMeditation : Feature
    {
        public override string Name => "Auto-Meditation";

        public override string Description => "Automatically use Meditation when out of combat.";

        public override FeatureType FeatureType => FeatureType.Actions;

        public override void Enable()
        {
            TaskManager.ShowDebug = false;
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        private void RunFeature(IFramework framework)
        {
            if (Player.Object is null) return;
            var isMonk = Player.Job == Job.MNK;
            var isPugilist = Player.Job == Job.PGL;
            if (!isMonk && !isPugilist) return;
            var gauge = Svc.Gauges.Get<MNKGauge>();
            if (gauge.Chakra == 5) return;

            if (!Svc.Condition[ConditionFlag.InCombat])
            {
                TaskManager.DelayNext(1000);
                TaskManager.Enqueue(() =>
                {
                    if (Svc.Condition[ConditionFlag.InCombat]) return;
                    if (Player.Level >= 54 && isMonk && Common.IsActionUnlocked(36942))
                    {
                        UseAction(36942);
                    }
                    else
                    {
                        UseAction(36940);
                    }
                });
            }
        }

        public override void Disable()
        {
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }
    }
}
