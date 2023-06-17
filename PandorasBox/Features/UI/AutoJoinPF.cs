using ClickLib.Clicks;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using PandorasBox.FeaturesSetup;
using System;
using System.Collections.Generic;
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

            // private readonly List<T> configList = new List<T>();
            // public T this[int index]
            // {
            //     get => configList[index];
            //     set => configList[index] = value;
            // }
            // public int Count => configList.Count;
            // public bool IsReadOnly => false;

            // public void Add(T item)
            // {
            //     configList.Add(item);
            // }

            // private void SetConfig(int index, T value)
            // {
            //     if (index >= 0 && index < configList.Count)
            //     {
            //         configList[index] = value;
            //     }
            // }
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
            // int numColumns = 4;
            // int numRows = (Config.Count + numColumns - 1) / numColumns;

            // ImGui.BeginTable("config_table", numColumns, ImGuiTableFlags.Borders);

            // for (int row = 0; row < numRows; row++)
            // {
            //     ImGui.TableNextRow();

            //     for (int col = 0; col < numColumns; col++)
            //     {
            //         int index = row * numColumns + col;

            //         if (index < Config.Count)
            //         {
            //             ImGui.TableSetColumnIndex(col);
            //             bool value = Config[index];
            //             ImGui.Checkbox($"##config_checkbox_{index}", ref value);
            //             Config[index] = value;
            //         }
            //     }
            // }
            // ImGui.EndTable();
        };
    }
}
