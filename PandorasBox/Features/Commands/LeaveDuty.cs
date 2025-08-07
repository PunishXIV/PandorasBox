using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;

namespace PandorasBox.Features.Commands
{
    internal class LeaveDuty : CommandFeature
    {
        public override string Name => "Leave Duty";
        public override string Command { get; set; } = "/pdfleave";

        public override string Description => "Quickly leaves a duty.";
        protected unsafe override void OnCommand(List<string> args)
        {
            if (GameMain.Instance()->CurrentContentFinderConditionId != 0 && !Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat])
            {
                Chat.Instance.SendMessage("/dfinder");
                if (Svc.GameGui.GetAddonByName("ContentsFinderMenu") != IntPtr.Zero)
                {
                    var ui = (AtkUnitBase*)Svc.GameGui.GetAddonByName("ContentsFinderMenu").Address;
                    Callback.Fire(ui, true, 0);
                    Callback.Fire(ui, false, -2);

                    var yesno = (AtkUnitBase*)Svc.GameGui.GetAddonByName("SelectYesno").Address;
                    Callback.Fire(yesno, true, 0);
                }
            }
            else
            {
                if (GameMain.Instance()->CurrentContentFinderConditionId == 0)
                {
                    Svc.Chat.PrintError("You are not in a duty to leave.");
                    return;
                }

                if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat])
                {
                    Svc.Chat.PrintError("Cannot leave during combat.");
                    return;
                }
            }
        }
    }
}
