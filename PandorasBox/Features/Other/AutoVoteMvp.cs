using Dalamud.Game;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using PandorasBox.FeaturesSetup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace PandorasBox.Features.Other;

public class AutoVoteMvp : Feature
{
    public override string Name => "Auto vote Mvp";

    public override string Description => "Auto vote the Mvp after duty for you.\nTank: Tank -> Healer -> DPS\nHealer: Healer -> Tank -> DPS\nDPS: DPS ->Tank -> Healer";

    public override FeatureType FeatureType => FeatureType.Other;

    public override bool UseAutoConfig => false;

    public override void Enable()
    {
        Svc.Framework.Update += FrameworkUpdate;
        base.Enable();
    }

    public override void Disable()
    {
        Svc.Framework.Update -= FrameworkUpdate;
        base.Disable();
    }

    private unsafe void FrameworkUpdate(Framework framework)
    {
        if (Player.Object == null) return;
        var bannerWindow = (AtkUnitBase*)Svc.GameGui.GetAddonByName("BannerMIP", 1);
        if (bannerWindow == null) return;

        try
        {
            VoteBanner(bannerWindow, ChoosePlayer());
        }
        catch (Exception e)
        {
            PluginLog.Error(e, "Failed to vote!");
        }
    }

    private static unsafe int ChoosePlayer()
    {
        var hud = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()
            ->GetUiModule()->GetAgentModule()->GetAgentHUD();

        if (hud == null) throw new Exception("HUD is empty!");

        var list = Svc.Party.Where(i =>
        i.ObjectId != Player.Object.ObjectId && i.GameObject != null)
            .Select(PartyMember => (Math.Max(0, GetPartySlotIndex(PartyMember.ObjectId, hud) - 1), PartyMember));

        if (!list.Any()) throw new Exception("Party list is empty! Can't vote anyone!");

        var tanks = list.Where(i => i.PartyMember.ClassJob.GameData.Role == 1);
        var healer = list.Where(i => i.PartyMember.ClassJob.GameData.Role == 4);
        var dps = list.Where(i => !(i.PartyMember.ClassJob.GameData.Role is 1 or 4));

        (int index, PartyMember member) voteTarget;
        switch (Player.Object.ClassJob.GameData.Role)
        {
            //tank
            case 1:
                if (tanks.Any()) voteTarget = RandomPick(tanks);
                else if (healer.Any()) voteTarget = RandomPick(healer);
                else voteTarget = RandomPick(dps);
                break;

            //Healer
            case 4:
                if (healer.Any()) voteTarget = RandomPick(healer);
                else if (tanks.Any()) voteTarget = RandomPick(tanks);
                else voteTarget = RandomPick(dps);
                break;

            //DPS
            default:
                if (dps.Any()) voteTarget = RandomPick(dps);
                else if (tanks.Any()) voteTarget = RandomPick(tanks);
                else voteTarget = RandomPick(healer);
                break;
        }

        if (voteTarget.member == null) throw new Exception("No members! Can't vote!");

        Svc.Chat.Print(new SeString(new List<Payload>()
        {
            new TextPayload("Vote to "),
            voteTarget.member.ClassJob.GameData.Role switch
            {
                1 => new IconPayload(BitmapFontIcon.Tank),
                4 => new IconPayload(BitmapFontIcon.Healer),
                _ => new IconPayload(BitmapFontIcon.DPS),
            },
            new PlayerPayload(voteTarget.member.Name.TextValue, voteTarget.member.World.GameData.RowId),
        }));
        return voteTarget.index;
    }

    static unsafe int GetPartySlotIndex(uint objectId, AgentHUD* hud)
    {
        var list = (HudPartyMember*)hud->PartyMemberList;
        for (var i = 0; i < hud->PartyMemberCount; i++)
        {
            if (list[i].ObjectId == objectId)
            {
                return i;
            }
        }

        return 0;
    }

    private static T RandomPick<T>(IEnumerable<T> list)
        => list.ElementAt(new Random().Next(list.Count()));

    private static unsafe void VoteBanner(AtkUnitBase* bannerWindow, int index)
    {
        var atkValues = (AtkValue*)Marshal.AllocHGlobal(2 * sizeof(AtkValue));
        atkValues[0].Type = atkValues[1].Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int;
        atkValues[0].Int = 12;
        atkValues[1].Int = index;
        try
        {
            bannerWindow->FireCallback(2, atkValues);
        }
        finally
        {
            Marshal.FreeHGlobal(new IntPtr(atkValues));
        }
    }
}
