using Dalamud.Game;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using PandorasBox.FeaturesSetup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace PandorasBox.Features.UI;

public class AutoVoteMvp : Feature
{
    public override string Name => "Auto-Commendation after Duty";

    public override string Description => "Automatically give a commendation to a random player in your party at the end of a duty.";

    public override FeatureType FeatureType => FeatureType.UI;

    public override bool UseAutoConfig => false;

    private List<string> PremadePartyID { get; set; } = new();

    private List<uint> DeadPlayers { get; set; } = new();

    private Dictionary<uint, int> DeathTracker { get; set; } = new();

    public class Configs : FeatureConfig
    {
        public int Priority = 0;

        public bool HideChat = false;

        public bool ExcludeDeaths = false;

        public int HowManyDeaths = 1;

        public bool ResetOnWipe = false;
    }

    public Configs Config { get; private set; }

    public override unsafe void Enable()
    {
        if (GameMain.Instance()->CurrentContentFinderConditionId != 0)
        {
            var payload = PandoraPayload.Payloads.ToList();
            payload.Add(new TextPayload(" [Auto-Commendation] Please note as this feature was enabled mid-duty, it may not operate correctly if you have queued into the duty with other players in your party before joining."));
            Svc.Chat.Print(new SeString(payload));
        }
        Config = LoadConfig<Configs>() ?? new Configs();
        Svc.Framework.Update += FrameworkUpdate;
        Svc.Condition.ConditionChange += UpdatePartyCache;
        base.Enable();
    }

    private void UpdatePartyCache(Dalamud.Game.ClientState.Conditions.ConditionFlag flag, bool value)
    {
        if (Svc.Condition.Any())
        {
            if (flag == Dalamud.Game.ClientState.Conditions.ConditionFlag.WaitingForDuty && value)
            {
                foreach (var partyMember in Svc.Party)
                {
                    PluginLog.Debug($"Adding {partyMember.Name.ExtractText()} {partyMember.ObjectId} to premade list");
                    PremadePartyID.Add(partyMember.Name.ExtractText());
                }

                var countRemaining =
                    Svc.Party.Where(i => i.ObjectId != Player.Object.ObjectId && i.GameObject != null && !PremadePartyID.Any(y => y == i.Name.ExtractText())).Count();

                PluginLog.Debug($"Party has {countRemaining} available to commend.");
            }

            if (flag == Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty && !value)
            {
                PremadePartyID.Clear();
            }
        }
    }

    public override void Disable()
    {
        SaveConfig(Config);
        Svc.Framework.Update -= FrameworkUpdate;
        Svc.Condition.ConditionChange -= UpdatePartyCache;
        base.Disable();
    }

    private unsafe void FrameworkUpdate(IFramework framework)
    {
        if (Player.Object == null) return;
        CheckForDeadPartyMembers();

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

    private void CheckForDeadPartyMembers()
    {
        if (Svc.Party.Any())
        {
            if (Config.ResetOnWipe && Svc.Party.All(x => x.GameObject?.IsDead == true))
            {
                DeathTracker.Clear();
            }

            foreach (var pm in Svc.Party)
            {
                if (pm.GameObject == null) continue;
                if (pm.ObjectId == Svc.ClientState.LocalPlayer.ObjectId) continue;
                if (pm.GameObject.IsDead)
                {
                    if (DeadPlayers.Contains(pm.ObjectId)) continue;
                    DeadPlayers.Add(pm.ObjectId);
                    if (DeathTracker.ContainsKey(pm.ObjectId))
                        DeathTracker[pm.ObjectId] += 1;
                    else
                        DeathTracker.TryAdd(pm.ObjectId, 1);

                }
                else
                {
                    DeadPlayers.Remove(pm.ObjectId);
                }
            }
        }
        else
        {
            DeathTracker.Clear();
            DeadPlayers.Clear();
        }
    }

    private unsafe int ChoosePlayer()
    {
        var hud = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()
            ->GetUiModule()->GetAgentModule()->GetAgentHUD();

        if (hud == null) throw new Exception("HUD is empty!");

        var list = Svc.Party.Where(i =>
        i.ObjectId != Player.Object.ObjectId && i.GameObject != null && !PremadePartyID.Any(y => y == i.Name.ExtractText()))
            .Select(PartyMember => (Math.Max(0, GetPartySlotIndex(PartyMember.ObjectId, hud) - 1), PartyMember))
            .ToList();

        if (!list.Any()) throw new Exception("No party members, skipping commend.");

        if (Config.ExcludeDeaths)
        {
            foreach (var deadPlayers in DeathTracker)
            {
                if (deadPlayers.Value >= Config.HowManyDeaths)
                {
                    list.RemoveAll(x => x.PartyMember.ObjectId == deadPlayers.Key);
                }
            }
        }

        var tanks = list.Where(i => i.PartyMember.ClassJob.GameData.Role == 1);
        var healer = list.Where(i => i.PartyMember.ClassJob.GameData.Role == 4);
        var dps = list.Where(i => i.PartyMember.ClassJob.GameData.Role is 2 or 3);

        (int index, PartyMember member) voteTarget = new();
        switch (Config.Priority)
        {
            //tank
            case 0:
                if (tanks.Any()) voteTarget = RandomPick(tanks);
                else if (healer.Any()) voteTarget = RandomPick(healer);
                else voteTarget = RandomPick(dps);
                break;
            //Healer
            case 1:
                if (healer.Any()) voteTarget = RandomPick(healer);
                else if (tanks.Any()) voteTarget = RandomPick(tanks);
                else voteTarget = RandomPick(dps);
                break;
            //DPS
            case 2:
                if (dps.Any()) voteTarget = RandomPick(dps);
                else if (tanks.Any()) voteTarget = RandomPick(tanks);
                else voteTarget = RandomPick(healer);
                break;
            //No Priority
            case 3:
                voteTarget = RandomPick(list);
                break;
        }

        if (voteTarget.member == null) throw new Exception("No members! Can't vote!");

        if (!Config.HideChat)
        {
            var payload = PandoraPayload.Payloads.ToList();
            payload.AddRange(new List<Payload>()
            {
                new TextPayload("Commend given to "),
                voteTarget.member.ClassJob.GameData.Role switch
                {
                    1 => new IconPayload(BitmapFontIcon.Tank),
                    4 => new IconPayload(BitmapFontIcon.Healer),
                    _ => new IconPayload(BitmapFontIcon.DPS),
                },
                new PlayerPayload(voteTarget.member.Name.TextValue, voteTarget.member.World.GameData.RowId),
            });
            Svc.Chat.Print(new SeString(payload));
        }

        return voteTarget.index;
    }

    private static unsafe int GetPartySlotIndex(uint objectId, AgentHUD* hud)
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
        => list.ElementAt(new Random().Next(list.Count() - 1));

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
            Marshal.FreeHGlobal(new nint(atkValues));
        }
    }

    protected override DrawConfigDelegate DrawConfigTree => (ref bool _) =>
    {
        bool hasChanged = false;
        if (ImGui.RadioButton("Prioritize Tank Vote", Config.Priority == 0))
        {
            Config.Priority = 0;
            hasChanged = true;
        }

        if (ImGui.RadioButton("Prioritize Healer Vote", Config.Priority == 1))
        {
            Config.Priority = 1;
            hasChanged = true;
        }

        if (ImGui.RadioButton("Prioritize DPS Vote", Config.Priority == 2))
        {
            Config.Priority = 2;
            hasChanged = true;
        }

        if (ImGui.RadioButton("No Priority", Config.Priority == 3))
        {
            Config.Priority = 3;
            hasChanged = true;
        }

        if (ImGui.Checkbox("Hide Chat Message", ref Config.HideChat))
            hasChanged = true;

        if (ImGui.Checkbox("Exclude Party Members That Die", ref Config.ExcludeDeaths))
            hasChanged = true;

        if (Config.ExcludeDeaths)
        {
            if (ImGuiEx.InputIntBounded("How Many Times?", ref Config.HowManyDeaths, 1, 100)) hasChanged = true;
            if (ImGui.Checkbox("Reset Death Tracker on Wipe", ref Config.ResetOnWipe)) hasChanged = true;
        }

        if (hasChanged)
            SaveConfig(Config);
    };
}
