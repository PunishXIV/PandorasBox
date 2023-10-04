using Dalamud.Game.ClientState.Conditions;
using ECommons;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using System.Linq;
using PlayerState = FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState;

namespace PandorasBox.Features.Actions
{
    public unsafe class AutoMountZoneChange : Feature
    {
        public override string Name => "Auto-Mount on Zone Change";

        public override string Description => "Mounts on zone change if not already mounted.";

        public override FeatureType FeatureType => FeatureType.Actions;

        public class Configs : FeatureConfig
        {
            public float ThrottleF = 0.1f;
            public uint SelectedMount = 0;
            public bool AbortIfMoving = false;
            public bool ExcludeHousing = false;
            public bool JumpAfterMount = false;
        }

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => false;

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.ClientState.TerritoryChanged += RunFeature;
            base.Enable();
        }

        private void RunFeature(ushort e)
        {
            if (!Svc.Data.GetExcelSheet<TerritoryType>().First(x => x.RowId == e).Mount) return;

            if (Svc.Data.GetExcelSheet<TerritoryType>().First(x => x.RowId == e).Bg.RawString.Contains("/hou/") && Config.ExcludeHousing) 
            {
                TaskManager.Abort();
                return;
            }
            TaskManager.Enqueue(() => NotBetweenAreas);
            TaskManager.DelayNext("MountTeleportTryMount", (int)(Config.ThrottleF * 1000));
            TaskManager.Enqueue(TryMount, 3000);
            TaskManager.Enqueue(() =>
            {
                if (Config.JumpAfterMount)
                {
                    TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.Mounted]);
                    TaskManager.DelayNext(50);
                    TaskManager.Enqueue(() => ActionManager.Instance()->UseAction(ActionType.GeneralAction, 2));
                    TaskManager.DelayNext(50);
                    TaskManager.Enqueue(() => ActionManager.Instance()->UseAction(ActionType.GeneralAction, 2));
                }
            });
        }

        private static bool NotBetweenAreas => !Svc.Condition[ConditionFlag.BetweenAreas];
        private bool? TryMount()
        {
            if (Svc.ClientState.LocalPlayer is null) return false;
            if (Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51]) return false;
            if (Svc.Condition[ConditionFlag.Mounted]) return true;

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
                if (am->GetActionStatus(ActionType.GeneralAction, 9) != 0) return false;
                am->UseAction(ActionType.GeneralAction, 9);

                return true;
            }

        }

        public override void Disable()
        {
            SaveConfig(Config);
            Svc.ClientState.TerritoryChanged -= RunFeature;
            base.Disable();
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
        {
            ImGui.PushItemWidth(300);
            if (ImGui.SliderFloat("Set Delay (seconds)", ref Config.ThrottleF, 0.1f, 10f, "%.1f")) hasChanged = true;
            var ps = PlayerState.Instance();
            var preview = Svc.Data.GetExcelSheet<Mount>().First(x => x.RowId == Config.SelectedMount).Singular.ExtractText().ToTitleCase();
            if (ImGui.BeginCombo("Select Mount", preview))
            {
                if (ImGui.Selectable("", Config.SelectedMount == 0))
                {
                    Config.SelectedMount = 0;
                    hasChanged = true;
                }

                foreach (var mount in Svc.Data.GetExcelSheet<Mount>().OrderBy(x => x.Singular.ExtractText()))
                {
                    if (ps->IsMountUnlocked(mount.RowId))
                    {
                        var selected = ImGui.Selectable(mount.Singular.ExtractText().ToTitleCase(), Config.SelectedMount == mount.RowId);

                        if (selected)
                        {
                            Config.SelectedMount = mount.RowId;
                            hasChanged = true;
                        }
                    }
                }

                ImGui.EndCombo();
            }

            if (ImGui.Checkbox("Abort if moving", ref Config.AbortIfMoving)) hasChanged = true;
            if (ImGui.Checkbox("Exclude Housing Zones", ref Config.ExcludeHousing)) hasChanged = true;
            if (ImGui.Checkbox("Jump after mounting", ref Config.JumpAfterMount)) hasChanged = true;

        };
    }
}
