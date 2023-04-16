using Dalamud.Logging;
using ECommons;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
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
    public unsafe class AutoMountZoneChange : Feature
    {
        public override string Name => "Auto-mount on Zone Change";

        public override string Description => "Uses Mount Roulette on zone change if not already mounted.";

        public override FeatureType FeatureType => FeatureType.Actions;

        public class Configs : FeatureConfig
        {
            public float ThrottleF = 0.1f;

            public uint SelectedMount = 0;

            public bool AbortIfMoving = false;

            public bool ExcludeHousing = false;
        }

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => false;

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.ClientState.TerritoryChanged += RunFeature;
            base.Enable();
        }

        private void RunFeature(object sender, ushort e)
        {
            if (Svc.Data.GetExcelSheet<TerritoryType>().First(x => x.RowId == e).Bg.RawString.Contains("/hou/") && Config.ExcludeHousing) 
            {
                TaskManager.Abort();
                return;
            }
            TaskManager.DelayNext("MountTeleport", 600);
            TaskManager.DelayNext("MountTeleport", (int)(Config.ThrottleF * 1000));
            TaskManager.Enqueue(TryMount);
        }

        private bool? TryMount()
        {
            if (Svc.ClientState.LocalPlayer is null) return false;
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas]) return false;
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Mounted]) return null;

            if (Config.AbortIfMoving && IsMoving()) return null;

            if (IsMoving()) return false;
            ActionManager* am = ActionManager.Instance();

            if (Config.SelectedMount > 0)
            {
                if (am->GetActionStatus(ActionType.Mount, Config.SelectedMount) != 0) return false;
                TaskManager.Enqueue(() => am->UseAction(ActionType.Mount, Config.SelectedMount));

                return true;
            }
            else
            {
                if (am->GetActionStatus(ActionType.General, 9) != 0) return false;
                TaskManager.Enqueue(() => am->UseAction(ActionType.General, 9));

                return true;
            }
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Svc.ClientState.TerritoryChanged -= RunFeature;
            base.Disable();
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool _) =>
        {
            ImGui.PushItemWidth(300);
            ImGui.SliderFloat("Set Delay (seconds)", ref Config.ThrottleF, 0.1f, 10f, "%.1f");
            var ps = PlayerState.Instance();
            string preview = Svc.Data.GetExcelSheet<Mount>().First(x => x.RowId == Config.SelectedMount).Singular.ExtractText().ToTitleCase();
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
                        bool selected = ImGui.Selectable(mount.Singular.ExtractText().ToTitleCase(), Config.SelectedMount == mount.RowId);

                        if (selected)
                        {
                            Config.SelectedMount = mount.RowId;
                        }
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.Checkbox("Abort if moving", ref Config.AbortIfMoving);
            ImGui.Checkbox("Exclude Housing Zones", ref Config.ExcludeHousing);

        };
    }
}
