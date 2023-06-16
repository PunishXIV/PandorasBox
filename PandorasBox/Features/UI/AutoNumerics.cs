using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface.Components;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using PandorasBox.FeaturesSetup;

namespace PandorasBox.Features.UI
{
    public unsafe class AutoNumerics : Feature
    {
        public override string Name => "Auto-Fill Numeric Dialogs";

        public override string Description => "Automatically confirms any numeric input dialog boxes (trading, FC chests, etc.)";

        public override FeatureType FeatureType => FeatureType.UI;

        public Configs Config { get; private set; }

        public class Configs : FeatureConfig
        {
            public int MinOrMax = 1;
            public bool ManualTradesOnly = false;
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }


        private void RunFeature(Dalamud.Game.Framework framework)
        {
            var numeric = (AtkUnitBase*)Svc.GameGui.GetAddonByName("InputNumeric");
            if (numeric == null) return;
            if (numeric->IsVisible)
            {
                try
                {
                    if (Config.MinOrMax != -1 && Config.ManualTradesOnly && Svc.Condition[ConditionFlag.TradeOpen])
                    {
                        var tradeAmt = numeric->UldManager.NodeList[4]->GetAsAtkComponentNode()->Component->UldManager.NodeList[4]->GetAsAtkTextNode()->NodeText.ToString();
                        int num;

                        if (int.TryParse(tradeAmt, out num) && num > 0 && !numeric->UldManager.NodeList[4]->GetAsAtkComponentNode()->Component->UldManager.NodeList[6]->IsVisible)
                            TaskManager.Enqueue(() => Callback.Fire(numeric, true, int.Parse(tradeAmt)));
                    }
                    else
                    {
                        if (Config.MinOrMax == 0)
                            TaskManager.Enqueue(() => Callback.Fire(numeric, true, numeric->AtkValues[2].Int));
                        if (Config.MinOrMax == 1)
                            TaskManager.Enqueue(() => Callback.Fire(numeric, true, numeric->AtkValues[3].Int));
                        if (Config.MinOrMax == -1)
                        {
                            var tradeAmt = numeric->UldManager.NodeList[4]->GetAsAtkComponentNode()->Component->UldManager.NodeList[4]->GetAsAtkTextNode()->NodeText.ToString();
                            int num;

                            if (int.TryParse(tradeAmt, out num) && num > 0 && !numeric->UldManager.NodeList[4]->GetAsAtkComponentNode()->Component->UldManager.NodeList[6]->IsVisible)
                                TaskManager.Enqueue(() => Callback.Fire(numeric, true, int.Parse(tradeAmt)));
                        }
                    }
                }
                catch
                {
                    return;
                }
            }
            else
            {
                TaskManager.Abort();
                return;
            }
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool _) =>
        {
            if (ImGui.RadioButton($"Auto confirm highest amount possible", Config.MinOrMax == 1))
            {
                Config.MinOrMax = 1;
            }
            if (ImGui.RadioButton($"Auto confirm lowest amount possible", Config.MinOrMax == 0))
            {
                Config.MinOrMax = 0;
            }
            if (ImGui.RadioButton($"Auto confirm manually entered amounts", Config.MinOrMax == -1))
            {
                Config.MinOrMax = -1;
            }
            ImGui.Checkbox("\"Auto confirm manually\" for trading only", ref Config.ManualTradesOnly);
            ImGuiComponents.HelpMarker("Will only do \"Auto confirm manually entered amounts\" for trading.\nDefaults to set highest/lowest behaviour for anything else.");
        };
    }
}
