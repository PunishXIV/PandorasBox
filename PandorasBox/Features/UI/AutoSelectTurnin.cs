using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using PandorasBox.FeaturesSetup;
using System;
using System.Collections.Generic;
using static ECommons.GenericHelpers;

namespace PandorasBox.Features.UI
{
    public unsafe class AutoSelectTurnin : Feature
    {
        public override string Name => "Auto-select Turn-ins";

        public override string Description => "Whenever you have to select an item to turn in, it will automatically fill in the interface.";

        public override FeatureType FeatureType => FeatureType.UI;

        private List<int> SlotsFilled { get; set; } = new();

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Automatically Confirm")]
            public bool AutoConfirm = false;
        }

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => true;

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        private void RunFeature(IFramework framework)
        {
            if (TryGetAddonByName<AddonRequest>("Request", out var addon))
            {
                for (var i = 1; i <= addon->EntryCount; i++)
                {
                    if (SlotsFilled.Contains(addon->EntryCount)) ConfirmOrAbort(addon);
                    if (SlotsFilled.Contains(i)) return;
                    var val = i;
                    TaskManager.EnqueueDelay(10);
                    TaskManager.Enqueue(() => TryClickItem(addon, val));
                }
            }
            else
            {
                SlotsFilled.Clear();
                TaskManager.Abort();
            }
        }

        private bool? TryClickItem(AddonRequest* addon, int i)
        {
            if (SlotsFilled.Contains(i)) return true;

            var contextMenu = (AtkUnitBase*)Svc.GameGui.GetAddonByName("ContextIconMenu", 1).Address;

            if (contextMenu is null || !contextMenu->IsVisible)
            {
                var slot = i - 1;
                var unk = (44 * i) + (i - 1);

                Callback.Fire(&addon->AtkUnitBase, false, 2, slot, 0, 0);

                return false;
            }
            else
            {
                Callback.Fire(contextMenu, false, 0, 0, 1021003, 0, 0);
                Svc.Log.Debug($"Filled slot {i}");
                SlotsFilled.Add(i);
                return true;
            }
        }

        private bool? ConfirmOrAbort(AddonRequest* addon)
        {
            if (!Config.AutoConfirm)
            {
                TaskManager.Abort();
                return true;
            }
            else
            {
                if (addon->HandOverButton != null && addon->HandOverButton->IsEnabled)
                {
                    new AddonMaster.Request((IntPtr)addon).HandOver();
                    return true;
                }
                return false;
            }
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Svc.Framework.Update -= RunFeature;
            SlotsFilled.Clear();
            base.Disable();
        }
    }
}
