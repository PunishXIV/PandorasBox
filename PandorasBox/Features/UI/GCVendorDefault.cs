using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using ECommons.Automation.UIInput;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using PandorasBox.FeaturesSetup;
using System.Collections.Generic;
using System.Linq;

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
            .Where(x => !string.IsNullOrEmpty(x.Name.ToString()))
            .Select(x => x.Name.ToString())
            .Append(Svc.Data.GetExcelSheet<Addon>().First(x => x.RowId == 518).Text.ToString())
            .ToList();

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "GrandCompanyExchange", PostSetup);
            base.Enable();
        }

        private void PostSetup(AddonEvent type, AddonArgs args)
        {
            var addon = (AtkUnitBase*)args.Addon.Address;
            TaskManager.EnqueueDelay(50);
            TaskManager.Enqueue(() => SelectRank(addon, Config.DefaultRank));
            TaskManager.EnqueueDelay(50);
            TaskManager.Enqueue(() => SelectTab(addon, Config.DefaultTab));
        }

        private static void SelectRank(AtkUnitBase* addon, int defaultRank)
        {
            var rankButton = defaultRank switch
            {
                0 => addon->GetNodeById(37)->GetAsAtkComponentRadioButton(),
                1 => addon->GetNodeById(38)->GetAsAtkComponentRadioButton(),
                2 => addon->GetNodeById(39)->GetAsAtkComponentRadioButton()
            };
            rankButton->ClickRadioButton((AtkComponentBase*)addon, (uint)defaultRank);
        }

        private static void SelectTab(AtkUnitBase* addon, int defaultTab)
        {
            foreach (var tabIndex in Enumerable.Range(0, 4))
            {
                var tabButton = tabIndex switch
                {
                    0 => addon->GetNodeById(46)->GetAsAtkComponentRadioButton(),
                    1 => addon->GetNodeById(44)->GetAsAtkComponentRadioButton(),
                    2 => addon->GetNodeById(45)->GetAsAtkComponentRadioButton(),
                    3 => addon->GetNodeById(47)->GetAsAtkComponentRadioButton()
                };
                var state = tabIndex == defaultTab;
                tabButton->IsSelected = state;
                tabButton->IsChecked = state;
                if (state)
                    tabButton->ClickRadioButton(addon);
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
            Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostSetup, "GrandCompanyExchange", PostSetup);
            base.Disable();
        }
    }
}
