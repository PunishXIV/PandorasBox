using ECommons.Automation;
using ECommons.DalamudServices;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using System.Collections.Generic;
using System.Linq;
using PandorasBox.Helpers;
using FFXIVClientStructs.FFXIV.Component.GUI;
using static ECommons.GenericHelpers;

namespace PandorasBox.Features.UI
{
    public unsafe class GCVendorDefault : Feature
    {
        public override string Name => "Default Grand Company Shop Menu";

        public override string Description => "Sets the default tab in the grand company menu when you open it.";

        public override FeatureType FeatureType => FeatureType.UI;

        public class Configs : FeatureConfig
        {
            public int DefaultRank = 0;
            public int DefaultTab = 0;
        }

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => false;

        private List<string> Tabs { get; set; } = new()
        {
            "Top Tab",
            "Middle Tab",
            "Bottom Tab"
        };

        private List<string> Categories { get; set; } = Svc.Data.GetExcelSheet<GCShopItemCategory>()
            .Where(x => !string.IsNullOrEmpty(x.Name.RawString))
            .Select(x => x.Name.RawString)
            .Append(Svc.Data.GetExcelSheet<Addon>().Where(x => x.RowId == 518).First().Text.RawString)
            .ToList();

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Common.OnAddonSetup += Common_AddonSetup;
            base.Enable();
        }


        private void Common_AddonSetup(SetupAddonArgs obj)
        {
            if (obj.AddonName == "GrandCompanyExchange")
            {
                var rankButton = Config.DefaultRank switch
                {
                    0 => obj.Addon->GetNodeById(37)->GetAsAtkComponentRadioButton(),
                    1 => obj.Addon->GetNodeById(38)->GetAsAtkComponentRadioButton(),
                    2 => obj.Addon->GetNodeById(39)->GetAsAtkComponentRadioButton()
                };

                var tabButton = Config.DefaultTab switch
                {
                    0 => obj.Addon->GetNodeById(46)->GetAsAtkComponentRadioButton(),
                    1 => obj.Addon->GetNodeById(44)->GetAsAtkComponentRadioButton(),
                    2 => obj.Addon->GetNodeById(45)->GetAsAtkComponentRadioButton(),
                    3 => obj.Addon->GetNodeById(47)->GetAsAtkComponentRadioButton(),
                };

                TaskManager.DelayNext(50);
                TaskManager.Enqueue(() => rankButton->ClickRadioButton((AtkComponentBase*)obj.Addon, (uint)Config.DefaultRank));
                TaskManager.DelayNext(50);
                uint param = Config.DefaultTab switch
                {
                    0 => 501,
                    1 => 502,
                    2 => 503,
                    3 => 505
                };

                TaskManager.Enqueue(() => tabButton->ClickRadioButton((AtkComponentBase*)obj.Addon, param));
                TaskManager.Enqueue(() => obj.Addon->Update(1f));
            }
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
        {
            var prevRank = Tabs[Config.DefaultRank];
            if (ImGui.BeginCombo("Select Rank", prevRank))
            {
                for (var i = 0; i < Tabs.Count; i++)
                {
                    if (ImGui.Selectable(Tabs[i], Config.DefaultRank == i))
                    {
                        Config.DefaultRank = i;
                        hasChanged = true;
                    }
                }

                ImGui.EndCombo();
            }

            var prevTab = Categories[Config.DefaultTab];
            if (ImGui.BeginCombo("Select Category", prevTab))
            {
                for (var i = 0; i < Categories.Count; i++)
                {
                    if (ImGui.Selectable(Categories[i], Config.DefaultTab == i))
                    {
                        Config.DefaultTab = i;
                        hasChanged = true;
                    }
                }
                ImGui.EndCombo();
            }

            if (hasChanged)
                SaveConfig(Config);
        };

        public override void Disable()
        {
            SaveConfig(Config);
            Common.OnAddonSetup -= Common_AddonSetup;
            base.Disable();
        }
    }
}
