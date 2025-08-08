using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Memory;
using Dalamud.Utility;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using PandorasBox.FeaturesSetup;
using System;
using System.Linq;

namespace PandorasBox.Features.UI
{
    public unsafe class QuickLowerQuality : Feature
    {
        public override string Name => "Quick Lower Quality";

        public override string Description => "Automatically confirms the lower quality popup menu.";

        public override FeatureType FeatureType => FeatureType.UI;

        public override void Enable()
        {
            Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "SelectYesno", PostSetup);
            base.Enable();
        }

        private void PostSetup(AddonEvent type, AddonArgs args)
        {
            var addon = (AtkUnitBase*)args.Addon.Address;
            var seString = MemoryHelper.ReadSeStringNullTerminated(new IntPtr(addon->AtkValues[0].String));
            if (seString.Payloads.Count < 3 || seString.Payloads[2] is not TextPayload payload2)
            {
                return;
            }
            var rawText = payload2.Text!.Trim();
            var lastSpace = rawText.LastIndexOf(' ');
            var trimmedText = lastSpace >= 0
                ? rawText.Remove(lastSpace).TrimEnd()
                : rawText.TrimEnd();
            var sheetText = Svc.Data.GetExcelSheet<Addon>()!.First(x => x.RowId == 155).Text.ToDalamudString().Payloads[2].ToString().Trim();

            if (sheetText == trimmedText)
            {
                var values = stackalloc AtkValue[5];
                values[0] = new AtkValue
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 0
                };
                addon->FireCallback(1, values, true);
            }
        }

        public override void Disable()
        {
            Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "SelectYesno", PostSetup);
            base.Disable();
        }
    }
}
