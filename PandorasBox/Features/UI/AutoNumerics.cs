using ClickLib.Clicks;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using PandorasBox.FeaturesSetup;
using System;
using static ECommons.GenericHelpers;

namespace PandorasBox.Features.UI
{
    public unsafe class AutoNumerics : Feature
    {
        public override string Name => "Auto-Numerics";

        public override string Description => "Automatically confirms any numeric input dialog boxes (trading, FC chests, etc.)";

        public override FeatureType FeatureType => FeatureType.UI;

        public Configs Config { get; private set; }

        public class Configs : FeatureConfig
        {
            public int MinOrMax = 1;
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        private void RunFeature(Dalamud.Game.Framework framework)
        {
            var numeric = (AtkUnitBase*)Svc.GameGui.GetAddonByName("InputNumeric", 1);
            if (numeric == null) return;
            if (numeric->IsVisible && TryGetAddonByName<AddonLookingForGroupDetail>("InputNumeric", out var addon))
            {
                if (Config.MinOrMax == 0)
                    TaskManager.Enqueue(() => Callback.Fire(&addon->AtkUnitBase, true, numeric->AtkValues[2].Int));
                else
                    TaskManager.Enqueue(() => Callback.Fire(&addon->AtkUnitBase, true, numeric->AtkValues[3].Int));
            }
            else
            {
                TaskManager.Abort();
                return;
            }
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool _) =>
        {
            if (ImGui.RadioButton($"Auto set at highest amount possible", Config.MinOrMax == 1))
            {
                Config.MinOrMax = 1;
            }
            if (ImGui.RadioButton($"Auto set at lowest amount possible", Config.MinOrMax == 0))
            {
                Config.MinOrMax = 0;
            }
        };
    }
}
