using Dalamud.Memory;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using PandorasBox.FeaturesSetup;
using System.Linq;
using static ECommons.GenericHelpers;
using Addon = Lumina.Excel.GeneratedSheets.Addon;

namespace PandorasBox.Features.UI
{
    public unsafe class AutoCloseMenus : Feature
    {
        public override string Name => "Auto-Close Menus";
        public override string Description => "Automatically closes specific menus whenever they appear.";

        public override FeatureType FeatureType => FeatureType.UI;

        public Configs Config { get; private set; }
        public override bool UseAutoConfig => true;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Entrust Duplicates Results", "", 1)]
            public bool doEntrustDuplicates = false;

            [FeatureConfigOption("Desynthesis Results", "", 2)]
            public bool doDesynthesis = false;

            [FeatureConfigOption("Aetherial Reduction Results", "", 3)]
            public bool doAetherialReduction = false;

            [FeatureConfigOption("Full Mail Notification", "", 4)]
            public bool doFullMail = false;
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }

        private void RunFeature(IFramework framework)
        {
            if (Config.doEntrustDuplicates && TryGetAddonByName<AtkUnitBase>("RetainerItemTransferProgress", out var retainerProgress))
            {
                // Successfully entrusted items.
                if (MemoryHelper.ReadSeStringNullTerminated(new nint(retainerProgress->AtkValues[0].String)).ToString() == Svc.Data.GetExcelSheet<Addon>().First(x => x.RowId == 13528).Text.ExtractText())
                {
                    Svc.Log.Info($"Closing {nameof(retainerProgress)}");
                    Callback.Fire(retainerProgress, true, -1);
                }
            }

            if (Config.doDesynthesis && TryGetAddonByName<AtkUnitBase>("SalvageResult", out var salvageResult))
            {
                // Desynthesis successful
                if (salvageResult->UldManager.NodeList[16]->GetAsAtkTextNode()->NodeText.ToString() == Svc.Data.GetExcelSheet<Addon>().First(x => x.RowId == 1835).Text.ExtractText())
                {
                    Svc.Log.Info($"Closing {nameof(salvageResult)}");
                    Callback.Fire(salvageResult, true, 1);
                }
            }

            if (Config.doDesynthesis && TryGetAddonByName<AtkUnitBase>("SalvageAutoDialog", out var salvageAutoResult))
            {
                // Desynthesis successful
                if (salvageAutoResult->AtkValues[17].Byte == 0)
                {
                    Svc.Log.Info($"Closing {nameof(salvageAutoResult)}");
                    Callback.Fire(salvageAutoResult, true, 1);
                }
            }

            if (Config.doAetherialReduction && TryGetAddonByName<AtkUnitBase>("PurifyResult", out var purifyResult))
            {
                // Aetherial Reduction successful
                if (purifyResult->UldManager.NodeList[17]->GetAsAtkTextNode()->NodeText.ToString() == Svc.Data.GetExcelSheet<Addon>().First(x => x.RowId == 2171).Text.ExtractText())
                {
                    Svc.Log.Info($"Closing {nameof(purifyResult)}");
                    Callback.Fire(purifyResult, true, -1);
                }
            }

            if (Config.doFullMail && TryGetAddonByName<AtkUnitBase>("SelectOk", out var fullMailSelectOk))
            {
                var addonText = MemoryHelper.ReadSeStringNullTerminated(new nint(fullMailSelectOk->AtkValues[0].String)).ToString();

                if (addonText == Svc.Data.GetExcelSheet<Addon>().First(x => x.RowId == 1856).Text.ExtractText() || addonText == Svc.Data.GetExcelSheet<Addon>().First(x => x.RowId == 1857).Text.ExtractText())
                {
                    Svc.Log.Info($"Closing {nameof(fullMailSelectOk)}");
                    Callback.Fire(fullMailSelectOk, true, 0);
                }
            }
        }
    }
}
