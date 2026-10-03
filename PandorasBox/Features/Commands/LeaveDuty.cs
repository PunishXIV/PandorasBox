using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System.Collections.Generic;
using static ECommons.GenericHelpers;

namespace PandorasBox.Features.Commands;

internal class LeaveDuty : CommandFeature {
    public override string Name => "Leave Duty";
    public override string Command { get; set; } = "/pdfleave";

    public override string Description => "Quickly leaves a duty.";
    protected override unsafe void OnCommand(List<string> args) {
        if (GameMain.Instance()->CurrentContentFinderConditionId != 0 && !Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat]) {
            Chat.SendMessage("/dfinder");
            if (TryGetAddonByName<AtkUnitBase>("ContentsFinderMenu", out var ui)) {
                Callback.Fire(ui, true, 0);
                Callback.Fire(ui, false, -2);

                if (TryGetAddonByName<AtkUnitBase>("SelectYesno", out var yesno))
                    Callback.Fire(yesno, true, 0);
            }
        }
        else {
            if (GameMain.Instance()->CurrentContentFinderConditionId == 0) {
                Svc.Chat.PrintError("You are not in a duty to leave.");
                return;
            }

            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat]) {
                Svc.Chat.PrintError("Cannot leave during combat.");
                return;
            }
        }
    }
}
