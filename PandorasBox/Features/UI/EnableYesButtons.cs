using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using PandorasBox.FeaturesSetup;
using static ECommons.GenericHelpers;

namespace PandorasBox.Features.UI
{
    public unsafe class EnableYesButtons : Feature
    {
        public override string Name => "Automatically Enabled Yes Buttons";

        public override string Description => "Sets the Yes button on Yes/No prompts to automatically be enabled if normally a checkbox needed checked.";

        public override FeatureType FeatureType => FeatureType.UI;

        public override void Enable()
        {
            Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, EnableButton);
            base.Enable();
        }

        private void EnableButton(AddonEvent type, AddonArgs args)
        {
            if (args.AddonName == "SelectYesno")
            {
                if (TryGetAddonByName<AddonSelectYesno>("SelectYesno", out var addon))
                {
                    if (!addon->YesButton->IsEnabled)
                    {
                        addon->YesButton->AtkComponentBase.SetEnabledState(true);
                        addon->AtkUnitBase.UldManager.NodeList[13]->ToggleVisibility(false);
                        addon->AtkUnitBase.SetFocusNode(addon->YesButton->AtkComponentBase.AtkResNode);
                    }
                }
            }
        }

        public override void Disable()
        {
            Svc.AddonLifecycle.UnregisterListener(EnableButton);
            base.Disable();
        }

    }
}
