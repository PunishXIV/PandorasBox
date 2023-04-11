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
using System.Collections.Generic;
using System.Linq;

namespace PandorasBox.Features.Actions
{
    public unsafe class AutoMountGathering : Feature
    {
        public override string Name => "Auto-Mount after Gathering";

        public override string Description => "Uses Mount Roulette or a specific mount upon finishing gathering from a node. Will try to execute for up to 3 seconds after the delay if moving.";

        public override FeatureType FeatureType => FeatureType.Actions;

        public class Configs : FeatureConfig
        {
            public int Throttle = 100;

            public uint SelectedMount = 0;

            public bool AbortIfMoving = false;
        }

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => false;
        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Condition.ConditionChange += RunFeature;
            base.Enable();
        }

        private void RunFeature(Dalamud.Game.ClientState.Conditions.ConditionFlag flag, bool value)
        {
            if (flag == Dalamud.Game.ClientState.Conditions.ConditionFlag.Gathering && !value)
            {
                TaskManager.Enqueue(() => EzThrottler.Throttle("GatherMount", Config.Throttle));
                TaskManager.Enqueue(() => EzThrottler.Check("GatherMount"));
                TaskManager.Enqueue(TryMount, 3000);
            }
        }

        private bool? TryMount()
        {
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
            Svc.Condition.ConditionChange -= RunFeature;
            base.Disable();
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool _) =>
        {
            ImGui.PushItemWidth(350);
            ImGui.SliderInt("Set Delay (ms)", ref Config.Throttle, 100, 10000);
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

        };
    }
}
