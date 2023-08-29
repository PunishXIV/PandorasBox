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
using Dalamud.Logging;

namespace PandorasBox.Features.Actions
{
    public unsafe class AutoCordial : Feature
    {
        public override string Name => "Auto-Cordial";

        public override string Description => "Automatically use a cordial when at a given threshold.";

        public override FeatureType FeatureType => FeatureType.Actions;

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => false;

        internal static readonly List<uint> cordialRowIDs = new() { 12669, 6141, 16911 };
        internal static (string Name, uint Id, ushort NQGP, ushort HQGP)[] cordials;

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

        private static bool WillOvercap(int gp_recovery)
        {
            return ((int)Svc.ClientState.LocalPlayer.CurrentGp + gp_recovery) > (int)Svc.ClientState.LocalPlayer.MaxGp;
        }

        private void RunFeature(Framework framework)
        {
            if (Svc.ClientState.LocalPlayer is null) return;
            if (!(Svc.ClientState.LocalPlayer.ClassJob.Id == 16 || Svc.ClientState.LocalPlayer.ClassJob.Id == 17) || (Svc.ClientState.LocalPlayer.ClassJob.Id == 18 && !Config.UseOnFisher)) return;
            if (!((Svc.ClientState.LocalPlayer.CurrentGp < Config.DefaultThreshold && Config.DirectionBelow) || (Svc.ClientState.LocalPlayer.CurrentGp > Config.DefaultThreshold && Config.DirectionAbove))) return;

            var am = ActionManager.Instance();

            for (var i = Config.InvertPriority ? cordials.Length - 1 : 0; Config.InvertPriority ? i >= 0 : i < cordials.Length; i += Config.InvertPriority ? -1 : 1)
            {
                if (am->GetActionStatus(ActionType.Item, cordials[i].Id) == 0)
                {
                    if (!Config.PreventOvercap || (Config.PreventOvercap && !WillOvercap((int)cordials[i].NQGP)))
                        am->UseAction(ActionType.Item, cordials[i].Id, a4: 65535);
                }
            }
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            cordials = Svc.Data.GetExcelSheet<Item>()
                .Where(row => cordialRowIDs.Any(num => num == row.RowId))
                .Select(row => (
                    Name: row.Name.RawString,
                    Id: row.RowId,
                    NQGP: row.ItemAction.Value.Data[0],
                    HQGP: row.ItemAction.Value.DataHQ[0]
                )).ToArray();
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
