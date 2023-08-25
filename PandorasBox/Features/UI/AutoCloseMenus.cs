using ClickLib.Clicks;
using Dalamud.Logging;
using Dalamud.Memory;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using System;
using System.Linq;
using System.Windows.Forms;
using static ECommons.GenericHelpers;

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
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        private void RunFeature(Dalamud.Game.Framework framework)
        {
            if (Config.doEntrustDuplicates)
            {
                var retainerProgress = (AtkUnitBase*)Svc.GameGui.GetAddonByName("RetainerItemTransferProgress");
                if (retainerProgress == null) return;

                // Successfully entrusted items.
                if (MemoryHelper.ReadSeStringNullTerminated(new nint(retainerProgress->AtkValues[0].String)).ToString() == Svc.Data.GetExcelSheet<Addon>().First(x => x.RowId == 13528).Text.ExtractText())
                {
                    PluginLog.Log("Closing Entrust Duplicates menu");
                    Callback.Fire(retainerProgress, true, -1);
                }
            }

            if (Config.doDesynthesis)
            {
                var salvageResult = (AtkUnitBase*)Svc.GameGui.GetAddonByName("SalvageResult");
                if (salvageResult == null) return;

                // Desynthesis successful
                if (salvageResult->UldManager.NodeList[16]->GetAsAtkTextNode()->NodeText.ToString() == Svc.Data.GetExcelSheet<Addon>().First(x => x.RowId == 1835).Text.ExtractText())
                {
                    PluginLog.Log("Closing Salvage Results menu");
                    Callback.Fire(salvageResult, true, 1);
                }
            }

            if (Config.doAetherialReduction)
            {
                var purifyResult = (AtkUnitBase*)Svc.GameGui.GetAddonByName("PurifyResult");
                if (purifyResult == null) return;

                // Aetherial Reduction successful
                if (purifyResult->UldManager.NodeList[17]->GetAsAtkTextNode()->NodeText.ToString() == Svc.Data.GetExcelSheet<Addon>().First(x => x.RowId == 2171).Text.ExtractText())
                {
                    PluginLog.Log("Closing Purify Results menu");
                    Callback.Fire(purifyResult, true, -1);
                }
            }
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }
    }
}
