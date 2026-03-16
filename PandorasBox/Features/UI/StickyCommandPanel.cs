using Dalamud.Bindings.ImGui;
using Dalamud.Game.ClientState.Conditions;
using ECommons;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Data.Parsing;
using PandorasBox.FeaturesSetup;
using System;
using System.Collections.Generic;
using System.Text;

namespace PandorasBox.Features.UI
{
    public unsafe class StickyCommandPanel : Feature
    {
        public override string Name => "Sticky Command Panel";

        public override string Description => "Re-open the command panel under certain conditions";

        public override FeatureType FeatureType => FeatureType.UI;

        public class Configs : FeatureConfig
        {
            public bool AutoOpenInDuty = false;
            public bool AutoOpenEverywhere = false;
        }

        public Configs Config { get; private set; }
        public override bool UseAutoConfig => false;


        public override void Enable()
        {
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        private void RunFeature(IFramework framework)
        {
            Config = LoadConfig<Configs>() ?? new Configs();

            if (GenericHelpers.TryGetAddonByName<AtkUnitBase>("QuickPanel", out var quickPanel))
            {
                TaskManager.Abort();
            }
            else
            {
                if (Config.AutoOpenEverywhere || (Svc.Condition[ConditionFlag.BoundByDuty] && Config.AutoOpenInDuty))
                {
                    TaskManager.Enqueue(() => CheckCommandPanel());
                    TaskManager.EnqueueDelay(300);
                }
            }
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
        {
            if (ImGui.Checkbox("Re-open when in duty", ref Config.AutoOpenInDuty))
                SaveConfig(Config);

            if (ImGui.Checkbox("Re-open everywhere", ref Config.AutoOpenEverywhere))
                SaveConfig(Config);
        };

        private void CheckCommandPanel()
        {
            var quickPanel = AgentQuickPanel.Instance();
            if (quickPanel == null)
                return;

            quickPanel->OpenPanel(AgentQuickPanel.Instance()->ActivePanel, false, false);
        }

        public override void Disable()
        {
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }
    }
}
