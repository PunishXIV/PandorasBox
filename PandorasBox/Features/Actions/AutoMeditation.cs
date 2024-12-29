using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.JobGauge.Types;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.EzHookManager;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using PandorasBox.FeaturesSetup;
using System;

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
            EzSignatureHelper.Initialize(this);
            Svc.Condition.ConditionChange += DelayOutOfCombat;
            base.Enable();
        }

        private void DelayOutOfCombat(ConditionFlag flag, bool value)
        {
            if (Player.Object is null) return;
            if (Player.Job != Job.MNK) return;
            if (Svc.Condition[ConditionFlag.InCombat]) return;

            if (flag == ConditionFlag.InCombat)
            {
                var gauge = Svc.Gauges.Get<MNKGauge>();
                TaskManager.Abort();
            }
        }

        private void RunFeature(IFramework framework)
        {
            TaskManager.Enqueue(() =>
            {
                if (Player.Object is null) return;
                if (Player.Job != Job.MNK) return;
                if (Svc.Condition[ConditionFlag.InCombat]) return;

                var gauge = Svc.Gauges.Get<MNKGauge>();
                if (gauge.Chakra != 5)
                {
                    UseAction(36942);
                }
            });
        }

        public override void Disable()
        {
            SendActionHook?.Disable();
            Svc.Condition.ConditionChange -= DelayOutOfCombat;
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }
    }
}
