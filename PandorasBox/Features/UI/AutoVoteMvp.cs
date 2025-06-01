using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Gui.ContextMenu;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Memory;
using Dalamud.Utility;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.Sheets;
using PandorasBox.FeaturesSetup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using static FFXIVClientStructs.FFXIV.Client.UI.AddonRelicNoteBook;

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

    private IContextMenu contextMenu;
    private static readonly SeString CommendString = new SeString(PandoraPayload.Payloads.ToArray()).Append(new TextPayload("Auto-Commend"));
    private static readonly SeString UncommendString = new SeString(PandoraPayload.Payloads.ToArray()).Append(new TextPayload("Undo Auto-Commend"));
    private static readonly SeString CommendExclusionString = new SeString(PandoraPayload.Payloads.ToArray()).Append(new TextPayload("Exclude from Commendations"));
    private static readonly SeString CommendInclusionString = new SeString(PandoraPayload.Payloads.ToArray()).Append(new TextPayload("Include in Commendations"));

    private string ManualCommendName;
    private List<string> CommendExclusions { get; set; } = new();

    public class Configs : FeatureConfig
    {
        public int Priority = 0;

        public bool HideChat = false;

        public bool ExcludeDeaths = false;

        public int HowManyDeaths = 1;

        public bool ResetOnWipe = false;

        public bool ShowContextMenu = true;
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
        contextMenu = Svc.ContextMenu;
        contextMenu.OnMenuOpened += CommendContextMenu;
        base.Enable();
    }

    private unsafe void CommendContextMenu(IMenuOpenedArgs args)
    {
        if (!Config.ShowContextMenu) return;
        if (!(args.AddonName == "_PartyList" || args.AddonName == "ChatLog")) return;
        if (Svc.ClientState.IsPvP) return;
        if (!Svc.Condition.Any(Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty)) return;
        var argItem = (MenuTargetDefault)args.Target;
        if (argItem == null) return;
        if (PremadePartyID.Any(y => y == argItem.TargetName)) return;
        if (!Svc.Party.Cast<IPartyMember>()
            .Any(m => ((FFXIVClientStructs.FFXIV.Client.Game.Group.PartyMember*)m.Address)->NameString == argItem.TargetName)) return;
        if (!(CommendExclusions?.Contains(argItem.TargetName) ?? false))
        {
            var item = CreateCommendPlayerMenuItem(argItem.TargetName);
            args.AddMenuItem(item);
        }
        if (ManualCommendName != argItem.TargetName)
        {
            var item2 = CreateCommendExclusionMenuItem(argItem.TargetName);
            args.AddMenuItem(item2);
        }
    }


    private MenuItem CreateCommendPlayerMenuItem(string name)
    {
        var menuItem = new MenuItem();
        // NOTE: Dalamud suggests setting menuItem.Prefix, but leaving unset for consistency with upstream.
        //menuItem.Prefix = Dalamud.Game.Text.SeIconChar.BoxedLetterP;
        if (ManualCommendName != name)
        {
            menuItem.Name = CommendString;
            menuItem.OnClicked += _ => TaskManager.Enqueue(() => SetManualCommendTarget(name));
        }
        else
        {
            menuItem.Name = UncommendString;
            menuItem.OnClicked += _ => TaskManager.Enqueue(() => ClearManualCommendTarget(name));
        }
        return menuItem;
    }


    private MenuItem CreateCommendExclusionMenuItem(string name)
    {
        var menuItem = new MenuItem();
        if (CommendExclusions != null && CommendExclusions.Contains(name))
        {
            menuItem.Name = CommendInclusionString;
            menuItem.OnClicked += _ => TaskManager.Enqueue(() => UndoCommendationExclusion(name));
        }
        else
        {
            menuItem.Name = CommendExclusionString;
            menuItem.OnClicked += _ => TaskManager.Enqueue(() => ExcludePlayerFromCommendations(name));
        }
        return menuItem;
    }


    private unsafe void SetManualCommendTarget(string name)
    {
        ManualCommendName = name;
        var payload = PandoraPayload.Payloads.ToList();
        payload.Add(new TextPayload($"{name} will be automatically commended at the end of your duty."));
        Svc.Chat.Print(new SeString(payload));
    }

    private unsafe void ClearManualCommendTarget(string name)
    {
        ManualCommendName = "";
        var payload = PandoraPayload.Payloads.ToList();
        payload.Add(new TextPayload($"{name} is no longer selected for automatic commendation."));
        Svc.Chat.Print(new SeString(payload));
    }

    private unsafe void ExcludePlayerFromCommendations(string name)
    {
        CommendExclusions.Add(name);
        var payload = PandoraPayload.Payloads.ToList();
        payload.Add(new TextPayload($"{name} will be excluded from commendation at the end of your duty."));
        Svc.Chat.Print(new SeString(payload));
    }

    private unsafe void UndoCommendationExclusion(string name)
    {
        CommendExclusions.Remove(name);
        var payload = PandoraPayload.Payloads.ToList();
        payload.Add(new TextPayload($"{name} has been removed from commendation exclusions."));
        Svc.Chat.Print(new SeString(payload));
    }


    private void UpdatePartyCache(Dalamud.Game.ClientState.Conditions.ConditionFlag flag, bool value)
    {
        if (Svc.Condition.Any())
        {
            if (flag == Dalamud.Game.ClientState.Conditions.ConditionFlag.WaitingForDuty && value)
            {
                foreach (var partyMember in Svc.Party)
                {
                    Svc.Log.Debug($"Adding {partyMember.Name.GetText()} {partyMember.ObjectId} to premade list");
                    PremadePartyID.Add(partyMember.Name.GetText());
                }
            }

            if (flag == Dalamud.Game.ClientState.Conditions.ConditionFlag.BoundByDuty && !value)
            {
                PremadePartyID.Clear();
                ManualCommendName = "";
                CommendExclusions.Clear();
            }
        }
    }

    public override void Disable()
    {
        SaveConfig(Config);
        Svc.Framework.Update -= FrameworkUpdate;
        Svc.Condition.ConditionChange -= UpdatePartyCache;
        contextMenu.OnMenuOpened -= CommendContextMenu;
        base.Disable();
    }

    private unsafe void FrameworkUpdate(IFramework framework)
    {
        if (Player.Object == null) return;
        if (Svc.ClientState.IsPvP) return;
        CheckForDeadPartyMembers();

        var bannerWindow = (AtkUnitBase*)Svc.GameGui.GetAddonByName("BannerMIP", 1);
        if (bannerWindow == null) return;

        try
        {
            VoteBanner(bannerWindow, ChoosePlayer(bannerWindow));
        }
        catch (Exception e)
        {
            Svc.Log.Error(e, "Failed to vote!");
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
                if (pm.ObjectId == Svc.ClientState.LocalPlayer.GameObjectId) continue;
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
            ManualCommendName = "";
            CommendExclusions.Clear();
        }
    }

    private unsafe int ChoosePlayer(AtkUnitBase* bannerWindow)
    {
        var hud = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()
            ->GetUIModule()->GetAgentModule()->GetAgentHUD();

        if (hud == null) throw new Exception("HUD is empty!");

        foreach (var member in Svc.Party.Cast<IPartyMember>())
        {
            var m2 = (FFXIVClientStructs.FFXIV.Client.Game.Group.PartyMember*)member.Address;
            var name = m2->NameString;
            Svc.Log.Debug($"{name} {m2->Flags.ToString("g")}");
        }

        var list = Svc.Party.Where(i =>
        i.ObjectId != Player.Object.GameObjectId && i.GameObject != null && !PremadePartyID.Any(y => y == i.Name.GetText()))
            .Select(PartyMember => (Math.Max(0, GetPartySlotIndex(PartyMember.ObjectId, hud) - 1), PartyMember))
            .ToList();
        var list = (validPartyMembers ?? new List<(int, IPartyMember)>()).ToList();

        if (!list.Any()) return -1;

        if (Config.ExcludeDeaths)
        {
            foreach (var deadPlayers in DeathTracker)
            {
                if (deadPlayers.Value >= Config.HowManyDeaths)
                {
                    list.RemoveAll(x => x.PartyMember.ObjectId == deadPlayers.Key && x.PartyMember.Name.ExtractText() != ManualCommendName);
                }
            }
        }

        if ((bool)(CommendExclusions?.Any()))
        {
            foreach (var excludedPlayer in CommendExclusions)
            {
                Svc.Log.Debug($"Removing {excludedPlayer} from commendation list.");
                list.RemoveAll(x => x.PartyMember.Name.ExtractText() == excludedPlayer);
            }
        }

        var tanks = list.Where(i => i.PartyMember.ClassJob.Value.Role == 1);
        var healer = list.Where(i => i.PartyMember.ClassJob.Value.Role == 4);
        var dps = list.Where(i => i.PartyMember.ClassJob.Value.Role is 2 or 3);

        if (!list.Any()) list = validPartyMembers; //In case list is empty, may as well start from scratch - will also help prevent ArgumentOutOfRangeException
        (int index, IPartyMember member) voteTarget = new();
        if (ManualCommendName != "")
        {
            voteTarget = list.First(i => i.PartyMember.Name.ExtractText() == ManualCommendName);
            Svc.Log.Debug($"Manual Commendation set for {ManualCommendName}");
        }
        else switch (Config.Priority)
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

        if (voteTarget.member == null) voteTarget = RandomPick(validPartyMembers); //Make attempt to randomly pick a valid party member worst case scenario
        if (voteTarget.member == null) return -1; //If attempt failed and value is still null, then return -1

        for (int i = 22; i <= 22 + 7; i++)
        {
            if (bannerWindow->AtkValues[i].Type != FFXIVClientStructs.FFXIV.Component.GUI.ValueType.String) continue;
            var name = bannerWindow->AtkValues[i].String;
            if (name == voteTarget.member.Name.TextValue)
            {
                if (!Config.HideChat)
                {
                    var payload = PandoraPayload.Payloads.ToList();
                    payload.AddRange(new List<Payload>()
                    {
                        new TextPayload("Commend given to "),
                        voteTarget.member.ClassJob.Value!.Role switch
                        {
                            1 => new IconPayload(BitmapFontIcon.Tank),
                            4 => new IconPayload(BitmapFontIcon.Healer),
                            _ => new IconPayload(BitmapFontIcon.DPS),
                        },
                        new PlayerPayload(voteTarget.member.Name.TextValue, voteTarget.member.World.Value!.RowId),
                    });
                    Svc.Chat.Print(new SeString(payload));
                }

                Svc.Log.Debug($"Commed {name} at index {i}, {bannerWindow->AtkValues[i - 14].UInt}");
                return (int)bannerWindow->AtkValues[i - 14].UInt;
            }
        }

        return -1;
    }

    private static unsafe int GetPartySlotIndex(uint GameObjectId, AgentHUD* hud)
    {
        var list = hud->PartyMembers;
        for (var i = 0; i < hud->PartyMemberCount; i++)
        {
            if (list[i].Object->GetGameObjectId() == GameObjectId)
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
        if (index == -1) return;
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

        if (ImGui.Checkbox("Show Context Menu", ref Config.ShowContextMenu))
            hasChanged = true;

        if (ImGui.Checkbox("Exclude Party Members That Die", ref Config.ExcludeDeaths))
            hasChanged = true;

        if (Config.ExcludeDeaths)
        {
            if (ImGui.DragInt("How Many Times?", ref Config.HowManyDeaths, 0.01f, 1, 100)) hasChanged = true;
            if (ImGui.Checkbox("Reset Death Tracker on Wipe", ref Config.ResetOnWipe)) hasChanged = true;
        }

        if (hasChanged)
            SaveConfig(Config);
    };
}
