using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;
using Dalamud.Memory;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandorasBox.Features.UI
{
    public unsafe class QuickLowerQuality : Feature
    {
        public override string Name => "Quick Lower Quality";

        public override string Description => "Automatically confirms the lower quality popup menu.";

        public override FeatureType FeatureType => FeatureType.UI;

        public override void Enable()
        {
            Common.OnAddonSetup += Common_AddonSetup;
            base.Enable();
        }

        private void Common_AddonSetup(SetupAddonArgs obj)
        {
            if (obj.AddonName == "SelectYesno")
            {
                var seString = MemoryHelper.ReadSeStringNullTerminated(new IntPtr(obj.Addon->AtkValues[0].String));
                var sheetText = Svc.Data.GetExcelSheet<Addon>().Where(x => x.RowId == 155).First().Text.Payloads[2].RawString.Trim();
                var rawText = ((TextPayload)seString.Payloads[2]).Text.Trim();
                var trimmedText = rawText.Remove(rawText.LastIndexOf(' ')).TrimEnd();

                if (sheetText == trimmedText)
                {
                    var values = stackalloc AtkValue[5];
                    values[0] = new AtkValue()
                    {
                        Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                        Int = 0
                    };
                    obj.Addon->FireCallback(1, values, (void*)1);
                }
            }
        }

        public override void Disable()
        {
            Common.OnAddonSetup -= Common_AddonSetup;
            base.Disable();
        }
    }
}
