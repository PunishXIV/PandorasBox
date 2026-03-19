using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using PandorasBox.FeaturesSetup;

namespace PandorasBox.Features.UI
{
    internal unsafe class StickyQuickPanel : Feature
    {
        public override string Name { get; } = "Sticky Command Panel";
        public override string Description { get; } = "Prevents the Command Panel from closing during load screens.";
        public override FeatureType FeatureType => FeatureType.UI;

        public override void Enable()
        {
            Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "QuickPanel", PreventCloseEvent);
            base.Enable();
        }

        public override void Disable()
        {
            Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "QuickPanel", PreventCloseEvent);
            base.Disable();
        }

        private void PreventCloseEvent(AddonEvent type, AddonArgs args)
        {
            var addon = (AtkUnitBase*)args.Addon.Address;
            addon->Flags1B4 |= 0x16;
            addon->DisableFocusability = true;
        }
    }
} 