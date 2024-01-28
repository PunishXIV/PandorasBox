using Dalamud.Game.ClientState.Conditions;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.MJI;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using System.Linq;

namespace PandorasBox.Features.Actions
{
    public unsafe class AutoMountGathering : Feature
    {
        public override string Name => "Auto-Mount after Gathering";

        public override string Description => "Mounts upon finishing gathering from a node. Will try to execute for up to 3 seconds after the delay if moving.";

        public override FeatureType FeatureType => FeatureType.Actions;

        public class Configs : FeatureConfig
        {
            public float ThrottleF = 0.1f;
            public uint SelectedMount = 0;
            public bool AbortIfMoving = false;
            public bool UseOnIsland = false;
            public bool JumpAfterMount = false;
            public bool MoveAfterMount = false;
        }

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => false;
        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Condition.ConditionChange += RunFeature;
            base.Enable();
        }

        private bool GatheredOnIsland(ConditionFlag flag, bool value)
        {
            return flag == ConditionFlag.OccupiedInQuestEvent && !value && MJIManager.Instance()->IsPlayerInSanctuary != 0;
        }

        private void RunFeature(ConditionFlag flag, bool value)
        {
            if (flag == ConditionFlag.Gathering && !value || (GatheredOnIsland(flag, value) && Config.UseOnIsland))
            {
                TaskManager.Enqueue(() => EzThrottler.Throttle("GatherMount", (int)(Config.ThrottleF * 1000)));
                TaskManager.Enqueue(() => EzThrottler.Check("GatherMount"));
                TaskManager.Enqueue(TryMount, 3000);
                TaskManager.Enqueue(() =>
                {
                    Svc.GameConfig.TryGet(Dalamud.Game.Config.UiControlOption.FlyingControlType, out uint type);
                    if (Config.JumpAfterMount && Svc.ClientState.LocalPlayer!.ClassJob.Id != 18)
                    {
                        TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.Mounted], 5000, true);
                        TaskManager.DelayNext(50);
                        TaskManager.Enqueue(() => ActionManager.Instance()->UseAction(ActionType.GeneralAction, 2));
                        if (type == 1)
                        {
                            TaskManager.DelayNext(50);
                            TaskManager.Enqueue(() => ActionManager.Instance()->UseAction(ActionType.GeneralAction, 2));
                        }
                    }

                    if (Config.MoveAfterMount)
                    {
                        TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.Mounted], 5000, true);
                        TaskManager.Enqueue(() => Svc.Condition[ConditionFlag.InFlight] || Svc.Condition[ConditionFlag.Diving], 2000);
                        TaskManager.Enqueue(() => { Chat.Instance.SendMessage("/automove on"); });
                    }
                });
            }
        }

        private bool? TryMount()
        {
            if (Config.AbortIfMoving && IsMoving()) return true;

            if (IsMoving()) return false;
            var am = ActionManager.Instance();

            if (Config.SelectedMount > 0)
            {
                if (am->GetActionStatus(ActionType.Mount, Config.SelectedMount) != 0) return false;
                TaskManager.Enqueue(() => am->UseAction(ActionType.Mount, Config.SelectedMount));

                return true;
            }
            else
            {
                if (am->GetActionStatus(ActionType.GeneralAction, 9) != 0) return false;
                TaskManager.Enqueue(() => am->UseAction(ActionType.GeneralAction, 9));

                return true;
            }
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Svc.Condition.ConditionChange -= RunFeature;
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
            if (ImGui.Checkbox("Use on Island Sanctuary", ref Config.UseOnIsland)) hasChanged = true;
            if (ImGui.Checkbox("Jump after mounting", ref Config.JumpAfterMount)) hasChanged = true;
            hasChanged |= ImGui.Checkbox("Start moving after mounting", ref Config.MoveAfterMount);
        };
    }
}
