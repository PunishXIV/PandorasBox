using ClickLib.Clicks;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using PandorasBox.FeaturesSetup;
using System;
using static ECommons.GenericHelpers;

namespace PandorasBox.Features.UI
{
    public unsafe class AutoJoinPF : Feature
    {
        public override string Name => "Auto-Join Party Finder Groups";

        public override string Description => "Whenever you click a Party Finder listing, this will bypass the description window and auto click the join button.";

        public override FeatureType FeatureType => FeatureType.UI;
        public override bool UseAutoConfig => true;
        public Configs Config { get; private set; }

        public class Configs : FeatureConfig
        {
            public bool JoinNone = false;
            public bool JoinDutyRoulette = false;
            public bool JoinDungeons = false;
            public bool JoinGuildhests = false;
            public bool JoinTrials = false;
            public bool JoinHighEnd = false;
            public bool JoinPvP = false;
            public bool JoinQuestBattles = false;
            public bool JoinFATEs = false;
            public bool JoinTreasureHunts = false;
            public bool JoinTheHunts = false;
            public bool JoinGatheringForays = false;
            public bool JoinDeepDungeons = false;
            public bool JoinFieldOperations = false;
            public bool JoinVC = false;
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        private void RunFeature(Dalamud.Game.Framework framework)
        {
            if (TryGetAddonByName<AddonLookingForGroupDetail>("LookingForGroupDetail", out var addon))
            {
                if (IsPrivatePF(addon) || IsSelfParty(addon)) { TaskManager.Abort(); return; }
                TaskManager.Enqueue(() => !(IsPrivatePF(addon) || IsSelfParty(addon)));
                TaskManager.DelayNext($"ClickingJoin", 300);
                TaskManager.Enqueue(() => Callback.Fire((AtkUnitBase*)addon, false, 0));
                TaskManager.Enqueue(() => ConfirmYesNo());
            }
            else
            {
                TaskManager.Abort();
            }
        }

        private bool IsPrivatePF(AddonLookingForGroupDetail* addon)
        {
            // 111 is the lock icon
            return addon->AtkUnitBase.UldManager.NodeList[111]->IsVisible;
        }

        private bool IsSelfParty(AddonLookingForGroupDetail* addon)
        {
            // 113 is the party host's name
            return addon->AtkUnitBase.UldManager.NodeList[113]->GetAsAtkTextNode()->NodeText.ToString() == Svc.ClientState.LocalPlayer.Name.TextValue;
        }

        private int GetPartyType()
        {
            var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("LookingForGroupDetail");
            var partyType = addon->AtkValues[16].Int;
            return partyType;
        }

        internal static bool ConfirmYesNo()
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied39]) return false;

            if (TryGetAddonByName<AddonLookingForGroupDetail>("LookingForGroupDetail", out var r) &&
                r->AtkUnitBase.IsVisible && TryGetAddonByName<AddonSelectYesno>("SelectYesno", out var addon) &&
                addon->AtkUnitBase.IsVisible &&
                addon->YesButton->IsEnabled &&
                addon->AtkUnitBase.UldManager.NodeList[15]->IsVisible)
            {
                new ClickSelectYesNo((IntPtr)addon).Yes();
                return true;
            }

            return false;
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool _) =>
        {
            if (ImGui.BeginTable("Party Types", 4))
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Checkbox("None", ref Config.JoinNone);
                ImGui.TableSetColumnIndex(1);
                ImGui.Checkbox("Duty Roulette", ref Config.JoinDutyRoulette);
                ImGui.TableSetColumnIndex(2);
                ImGui.Checkbox("Dungeons", ref Config.JoinDungeons);
                ImGui.TableSetColumnIndex(3);
                ImGui.Checkbox("Guildhests", ref Config.JoinGuildhests);
                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                ImGui.Checkbox("Trials", ref Config.JoinTrials);
                ImGui.TableSetColumnIndex(1);
                ImGui.Checkbox("High End", ref Config.JoinHighEnd);
                ImGui.TableSetColumnIndex(2);
                ImGui.Checkbox("PvP", ref Config.JoinPvP);
                ImGui.TableSetColumnIndex(3);
                ImGui.Checkbox("Quest Battles", ref Config.JoinQuestBattles);
                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                ImGui.Checkbox("FATEs", ref Config.JoinFATEs);
                ImGui.TableSetColumnIndex(1);
                ImGui.Checkbox("Treasure Hunts", ref Config.JoinTreasureHunts);
                ImGui.TableSetColumnIndex(2);
                ImGui.Checkbox("The Hunt", ref Config.JoinTheHunts);
                ImGui.TableSetColumnIndex(3);
                ImGui.Checkbox("Gathering Forays", ref Config.JoinGatheringForays);
                ImGui.TableNextRow();

                ImGui.TableSetColumnIndex(0);
                ImGui.Checkbox("Deep Dungeons", ref Config.JoinDeepDungeons);
                ImGui.TableSetColumnIndex(1);
                ImGui.Checkbox("Field Operations", ref Config.JoinFieldOperations);
                ImGui.TableSetColumnIndex(2);
                ImGui.Checkbox("V&C", ref Config.JoinVC);
                ImGui.EndTable();
            }
            // ImGui.Checkbox("Hide Chat Message", ref Config.JoinTrials);
            // ImGui.Checkbox("Hide Chat Message", ref Config.JoinHighEnd);
            // ImGui.Checkbox("Hide Chat Message", ref Config.JoinPvP);
            // ImGui.Checkbox("Hide Chat Message", ref Config.JoinQuestBattles);
            // ImGui.Checkbox("Hide Chat Message", ref Config.JoinFATEs);
            // ImGui.Checkbox("Hide Chat Message", ref Config.JoinTreasureHunts);
            // ImGui.Checkbox("Hide Chat Message", ref Config.JoinTheHunts);
            // ImGui.Checkbox("Hide Chat Message", ref Config.JoinGatheringForays);
            // ImGui.Checkbox("Hide Chat Message", ref Config.JoinDeepDungeons);
            // ImGui.Checkbox("Hide Chat Message", ref Config.JoinFieldOperations);
            // ImGui.Checkbox("Hide Chat Message", ref Config.JoinVC);
        };
    }
}
