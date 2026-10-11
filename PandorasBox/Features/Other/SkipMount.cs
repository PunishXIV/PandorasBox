using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Shell;
using PandorasBox.FeaturesSetup;

namespace PandorasBox.Features.Other;

internal class SkipMount : Feature {
    public override string Name => "Skip '/mount' In Macros If Mounted";

    public override string Description => "Using a macro with /mount in it will skip that line if you're already mounted. Also works when macroing mount roulettes";

    public override FeatureType FeatureType => FeatureType.Other;

    public override unsafe bool UseActionDetour(ActionManager* actionManager, ActionType actionType, uint actionId, ulong targetId, uint extraParam, ActionManager.UseActionMode mode, uint comboRouteId, bool* outOptAreaTargeted) {
        var macroLine = RaptureShellModule.Instance()->MacroCurrentLine;
        if (macroLine > 0) {
            var line = RaptureShellModule.Instance()->MacroLineText;
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Mounted]) {
                if (line.ToString().StartsWith("/mount"))
                    return false;

                if (line.ToString().StartsWith("/ac")) {
                    if (actionType == ActionType.GeneralAction && actionId is 9 or 24) // mount roulettes
                        return false;
                }
            }
        }
        return base.UseActionDetour(actionManager, actionType, actionId, targetId, extraParam, mode, comboRouteId, outOptAreaTargeted);
    }
}
