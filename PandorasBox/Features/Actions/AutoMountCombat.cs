using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Logging;
using ECommons;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using System;
using System.Linq;
using System.Text;
using PlayerState = FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState;

namespace PandorasBox.Features.Actions
{
    public unsafe class AutoMountCombat : Feature
    {
        public override string Name => "Auto-Mount After Combat ";

        public override string Description => "Mounts upon ending combat.";

        public override FeatureType FeatureType => FeatureType.Actions;

        public class Configs : FeatureConfig
        {
            public float ThrottleF = 0.1f;
            public uint SelectedMount = 0;
            public bool AbortIfMoving = false;
            public bool DisableInFates = true;
            public bool ExcludeHousing = false;
        }

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => false;

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Condition.ConditionChange += RunFeature;
            base.Enable();
        }

        private void RunFeature(ConditionFlag flag, bool value)
        {
            if (flag == ConditionFlag.InCombat && !value)
            {
                    TaskManager.Enqueue(() => NotInCombat);
                    TaskManager.DelayNext("CombatOverTryMount", (int)(Config.ThrottleF * 1000));
                    TaskManager.Enqueue(() => TryMount());
            }
        }

        private static bool NotInCombat => !Svc.Condition[ConditionFlag.InCombat];
        private bool? TryMount()
        {
            if (Svc.ClientState.LocalPlayer is null) return false;
            if (Svc.Condition[ConditionFlag.InCombat]) return false;
            if (Config.DisableInFates && FateManager.Instance()->CurrentFate != null) return false;
            if (Svc.Condition[ConditionFlag.Mounted]) return true;
            if (!Svc.Data.GetExcelSheet<TerritoryType>().First(x => x.RowId == Svc.ClientState.TerritoryType).Mount) return false;

            if (Svc.Data.GetExcelSheet<TerritoryType>().First(x => x.RowId == Svc.ClientState.TerritoryType).Bg.RawString.Contains("/hou/") && Config.ExcludeHousing)
            {
                TaskManager.Abort();
                return false;
            }

            if (Config.AbortIfMoving && IsMoving()) return true;

            if (IsMoving()) return false;
            var am = ActionManager.Instance();

            if (Config.SelectedMount > 0)
            {
                if (am->GetActionStatus(ActionType.Mount, Config.SelectedMount) != 0) return false;
                am->UseAction(ActionType.Mount, Config.SelectedMount);

                return true;
            }
            else
            {
                if (am->GetActionStatus(ActionType.General, 9) != 0) return false;
                am->UseAction(ActionType.General, 9);

                return true;
            }

        }

        public override void Disable()
        {
            SaveConfig(Config);
            Svc.Condition.ConditionChange -= RunFeature;
            base.Disable();
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool _) =>
        {
            ImGui.PushItemWidth(300);
            ImGui.SliderFloat("Set Delay (seconds)", ref Config.ThrottleF, 0.1f, 10f, "%.1f");
            var ps = PlayerState.Instance();
            var preview = Svc.Data.GetExcelSheet<Mount>().First(x => x.RowId == Config.SelectedMount).Singular.ExtractText().ToTitleCase();
            if (ImGui.BeginCombo("Select Mount", preview))
            {
                if (ImGui.Selectable("", Config.SelectedMount == 0))
                {
                    Config.SelectedMount = 0;
                }

                foreach (var mount in Svc.Data.GetExcelSheet<Mount>().OrderBy(x => x.Singular.ExtractText()))
                {
                    if (ps->IsMountUnlocked(mount.RowId))
                    {
                        var selected = ImGui.Selectable(mount.Singular.ExtractText().ToTitleCase(), Config.SelectedMount == mount.RowId);

                        if (selected)
                        {
                            Config.SelectedMount = mount.RowId;
                        }
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.Checkbox("Abort if moving", ref Config.AbortIfMoving);
            ImGui.Checkbox("Disable in fates", ref Config.DisableInFates);
            ImGui.Checkbox("Exclude Housing Zones", ref Config.ExcludeHousing);

        };
    }
}
