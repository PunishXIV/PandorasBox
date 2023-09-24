using Dalamud.Game;
using Dalamud.Logging;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using PandorasBox.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ECommons.GenericHelpers;

namespace PandorasBox.Features.UI
{
    public unsafe class DiscardUniqueReplacement : Feature
    {
        public override string Name => "Automatically Enabled Yes Buttons";

        public override string Description => "Sets the Yes button on Yes/No prompts to automatically be enabled if normally a checkbox needed checked.";

        public override FeatureType FeatureType => FeatureType.UI;

        public override void Enable()
        {
            Common.OnAddonSetup += EnableButton;
            base.Enable();
        }

        private void EnableButton(SetupAddonArgs args)
        {
            if (args.AddonName == "SelectYesno")
            {
                if (TryGetAddonByName<AddonSelectYesno>("SelectYesno", out var addon))
                {
                    if (!addon->YesButton->IsEnabled)
                    {
                        addon->YesButton->AtkComponentBase.SetEnabledState(true);
                        addon->AtkUnitBase.UldManager.NodeList[13]->ToggleVisibility(false);
                    }
                }
            }
        }

        public override void Disable()
        {
            Common.OnAddonSetup -= EnableButton;
            base.Disable();
        }

    }
}
