using Dalamud.Logging;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using PandorasBox.FeaturesSetup;
using System.Collections.Generic;
using static ECommons.GenericHelpers;

namespace PandorasBox.Features.UI
{
    public unsafe class AutoSelectTurnin : Feature
    {
        public override string Name => "Auto-select Turn-ins";

        public override string Description => "Whenever you have to select an item to turn in, it will automatically fill in the interface.";

        public override FeatureType FeatureType => FeatureType.UI;

        List<int> SlotsFilled = new();
        public override void Enable()
        {
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        private void RunFeature(Dalamud.Game.Framework framework)
        {

            if (TryGetAddonByName<AddonRequest>("Request", out var addon))
            {
                for (int i = 1; i <= addon->EntryCount; i++)
                {
                    if (SlotsFilled.Contains(addon->EntryCount)) TaskManager.Abort();
                    if (SlotsFilled.Contains(i)) return;
                    int val = i;
                    TaskManager.DelayNext($"ClickTurnin{val}", 10);
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

            var contextMenu = (AtkUnitBase*)Svc.GameGui.GetAddonByName("ContextIconMenu", 1);

            if (contextMenu is null ||  !contextMenu->IsVisible)
            {
                int slot = i - 1;
                int unk = (44 * i) + (i - 1);

                Callback.Fire(&addon->AtkUnitBase,false, 2, slot, 0, 0);

                return false;
            }
            else
            {
                Callback.Fire(contextMenu, false, 0, 0, 1021003, 0, 0);
                PluginLog.Debug($"Filled slot {i}");
                SlotsFilled.Add(i);
                return true;
            }
        }

        public override void Disable()
        {
            Svc.Framework.Update -= RunFeature;
            SlotsFilled.Clear();
            base.Disable();
        }
    }
}
