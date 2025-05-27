using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using PandorasBox.FeaturesSetup;
using System;
using static ECommons.GenericHelpers;

namespace PandorasBox.Features.UI
{
    public unsafe class AutoJoinPF : Feature
    {
        public override string Name => "Auto-Join Party Finder Groups";

        public override string Description => "Whenever you click a Party Finder listing, this will bypass the description window and auto click the join button.";

        public override FeatureType FeatureType => FeatureType.UI;

        public override void Enable()
        {
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        private void RunFeature(IFramework framework)
        {
            if (TryGetAddonByName<AddonLookingForGroupDetail>("LookingForGroupDetail", out var addon))
            {
                if (IsPrivatePF(addon) || IsSelfParty(addon)) { TaskManager.Abort(); return; }
                TaskManager.Enqueue(() => !(IsPrivatePF(addon) || IsSelfParty(addon)));
                TaskManager.EnqueueDelay(300);
                TaskManager.Enqueue(() => Callback.Fire((AtkUnitBase*)addon, false, 0));
                TaskManager.Enqueue(() => ConfirmYesNo());
            }
            else
            {
                TaskManager.Abort();
            }
        }

        private bool IsPrivatePF(AddonLookingForGroupDetail* addon)
        {
            // 111 is the lock icon
            return addon->AtkUnitBase.UldManager.NodeList[111]->IsVisible();
        }

        private bool IsSelfParty(AddonLookingForGroupDetail* addon)
        {
            // 113 is the party host's name
            return addon->AtkUnitBase.UldManager.NodeList[113]->GetAsAtkTextNode()->NodeText.ToString() == Svc.ClientState.LocalPlayer.Name.TextValue;
        }

        internal static bool ConfirmYesNo()
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied39]) return false;

            if (TryGetAddonByName<AddonLookingForGroupDetail>("LookingForGroupDetail", out var r) &&
                r->AtkUnitBase.IsVisible && TryGetAddonByName<AddonSelectYesno>("SelectYesno", out var addon) &&
                addon->AtkUnitBase.IsVisible &&
                addon->YesButton->IsEnabled &&
                addon->AtkUnitBase.UldManager.NodeList[15]->IsVisible())
            {
                new AddonMaster.SelectYesno((IntPtr)addon).Yes();
                return true;
            }

            return false;
        }

        public override void Disable()
        {
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }
    }
}
