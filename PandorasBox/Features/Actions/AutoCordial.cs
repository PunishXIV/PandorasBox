using PandorasBox.Features;
using PandorasBox.FeaturesSetup;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using System.Linq;
using ECommons.Throttlers;
using ImGuiNET;
using System.Collections.Generic;
using Lumina.Excel.GeneratedSheets;
using System.Text.RegularExpressions;

namespace PandorasBox.Features.Actions
{
    public unsafe class AutoCordial : Feature
    {
        public override string Name => "Auto-Cordial";

        public override string Description => "Automatically use a cordial when below the threshold.";

        public override FeatureType FeatureType => FeatureType.Actions;

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => false;

        // public Dictionary<uint, Item> Cordials { get; set; }

        private static uint hi_cordial = 12669;
        private static uint reg_cordial = 6141;
        private static uint reg_cordial_hq = 1006141;
        private static uint watered_cordial = 16911;
        private static uint watered_cordial_hq = 1016911;
        private List<uint> cordials = new List<uint>() { hi_cordial, reg_cordial_hq, reg_cordial, watered_cordial_hq, watered_cordial };
        private static int hi_recovery = 400;
        private static int reg_recovery_hq = 350;
        private static int reg_recovery = 300;
        private static int water_recovery_hq = 200;
        private static int water_recovery = 150;
        private List<int> recoveries = new List<int>() { hi_recovery, reg_recovery_hq, reg_recovery, water_recovery_hq, water_recovery };

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("GP Threshold", IntMin = 0, IntMax = 1000, EditorSize = 300)]
            public int DefaultThreshold = 1;
            public bool DirectionAbove = true;
            public bool DirectionBelow = false;

            [FeatureConfigOption("Invert Priority (Watered -> Regular -> Hi)")]
            public bool InvertPriority = false;

            [FeatureConfigOption("Prevent Overcap")]
            public bool PreventOvercap = true;

            [FeatureConfigOption("Use on Fisher")]
            public bool UseOnFisher = false;
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool _) =>
        {
            ImGui.PushItemWidth(300);
            ImGui.SliderInt("GP Threshold", ref Config.DefaultThreshold, 1, 1000);
            if (ImGui.RadioButton("Above", Config.DirectionAbove))
            {
                Config.DirectionAbove = true;
                Config.DirectionBelow = false;
            }
            ImGui.SameLine();
            if (ImGui.RadioButton("Below", Config.DirectionBelow))
            {
                Config.DirectionAbove = false;
                Config.DirectionBelow = true;
            }
            ImGui.Checkbox("Invert Priority (Watered -> Regular -> Hi)", ref Config.InvertPriority);
            ImGui.Checkbox("Prevent Overcap", ref Config.PreventOvercap);
            ImGui.Checkbox("Use on Fisher", ref Config.UseOnFisher);
        };

        private bool WillOvercap(int gp_recovery)
        {
            return Svc.ClientState.LocalPlayer.CurrentGp + gp_recovery < Svc.ClientState.LocalPlayer.MaxGp;
            // return Svc.ClientState.LocalPlayer.CurrentGp + (int)Regex.Match(Cordials.Description, @"\d+").value < Svc.ClientState.LocalPlayer.MaxGp;
        }

        private void RunFeature(Framework framework)
        {
            if (Svc.ClientState.LocalPlayer is null) return;
            if (!(Svc.ClientState.LocalPlayer.ClassJob.Id == 16) || !(Svc.ClientState.LocalPlayer.ClassJob.Id == 17)) return;
            if (Svc.ClientState.LocalPlayer.ClassJob.Id == 18 && !Config.UseOnFisher) return;

            if (!((Svc.ClientState.LocalPlayer.CurrentGp < Config.DefaultThreshold && Config.DirectionBelow) || (Svc.ClientState.LocalPlayer.CurrentGp > Config.DefaultThreshold && Config.DirectionAbove))) return;

            ActionManager* am = ActionManager.Instance();

            if (!Config.InvertPriority)
            {
                // static values version
                for (int i = 0; i < cordials.Count; i++)
                {
                    if (am->GetActionStatus(ActionType.Item, cordials[i]) == 0)
                    {
                        if (Config.PreventOvercap && !WillOvercap(recoveries[i]) || !Config.PreventOvercap)
                        {
                            am->UseAction(ActionType.Item, cordials[i], a4: 65535);
                        }
                    }
                }

                // sheets version
                // for (int i = 0; i < Cordials.Count; i++)
                // {
                //     if (am->GetActionStatus(ActionType.Item, Cordials[i].RowId) == 0)
                //     {
                //         if (Config.PreventOvercap && !WillOvercap(Cordials) || !Config.PreventOvercap)
                //         {
                //             am->UseAction(ActionType.Item, cordials[i], a4: 65535);
                //         }
                //     }
                // }
            }
            else
            {
                for (int i = cordials.Count - 1; i >= 0; i--)
                {
                    if (am->GetActionStatus(ActionType.Item, cordials[i]) == 0)
                    {
                        if (Config.PreventOvercap && !WillOvercap(recoveries[i]) || !Config.PreventOvercap)
                        {
                            am->UseAction(ActionType.Item, cordials[i], a4: 65535);
                        }
                    }
                }
            }
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            // Cordials = Svc.Data.GetExcelSheet<Item>().Where(x => x.Singular.ToString().Contains("cordial")).ToDictionary(x => x.RowId, x => x);
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        public override void Disable()
        {
            Svc.Framework.Update -= RunFeature;
            SaveConfig(Config);
            base.Disable();
        }
    }
}
