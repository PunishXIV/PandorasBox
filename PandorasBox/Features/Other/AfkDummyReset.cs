using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using PandorasBox.FeaturesSetup;
using System;

namespace PandorasBox.Features.Other;

internal class AfkDummyReset : Feature {
    public override string Name => "Inactivity Dummy Reset";

    public override string Description => "Automatically reset enmity on target dummies if you do not perform an action after a specified amount of time.";

    public override FeatureType FeatureType => FeatureType.Other;

    public override bool UseAutoConfig => true;

    public class Configs : FeatureConfig {
        [FeatureConfigOption("Inactivity Timer (seconds)", EditorSize = 300, IntMax = 120, IntMin = 1, EnforcedLimit = true)]
        public int InactivityTimer = 1;
    }

    public Configs Config { get; private set; } = null!;

    public override void Enable() {
        Config = LoadConfig<Configs>() ?? new Configs();
        base.Enable();
    }

    public override unsafe bool UseActionDetour(ActionManager* actionManager, ActionType actionType, uint actionId, ulong targetId, uint extraParam, ActionManager.UseActionMode mode, uint comboRouteId, bool* outOptAreaTargeted) {
        if (actionType is ActionType.Action) {
            try {
                if (TaskManager.IsBusy) {
                    TaskManager.Abort();
                }

                var delay = Config.InactivityTimer * 1000 + Svc.Data.GetExcelSheet<Lumina.Excel.Sheets.Action>().GetRow(actionManager->GetAdjustedActionId(actionId)).Cast100ms * 100;
                TaskManager.EnqueueDelay(delay);
                TaskManager.Enqueue(() => { Chat.SendMessage("/presetenmity"); });
            }
            catch (Exception ex) {
                ex.Log();
            }
        }
        return base.UseActionDetour(actionManager, actionType, actionId, targetId, extraParam, mode, comboRouteId, outOptAreaTargeted);
    }

    public override void Disable() {
        SaveConfig(Config);
        base.Disable();
    }
}
