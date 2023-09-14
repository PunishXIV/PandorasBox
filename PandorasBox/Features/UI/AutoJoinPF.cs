using ClickLib.Clicks;
using Dalamud.Memory;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using PandorasBox.FeaturesSetup;
using System;
using System.Linq;
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
            [FeatureConfigOption("None", "", 1)]
            public bool JoinNone = false;

            [FeatureConfigOption("Duty Roulette", "", 2)]
            public bool JoinDutyRoulette = false;

            [FeatureConfigOption("Dungeons", "", 3)]
            public bool JoinDungeons = false;

            [FeatureConfigOption("Guildhests", "", 4)]
            public bool JoinGuildhests = false;

            [FeatureConfigOption("Trials", "", 5)]
            public bool JoinTrials = false;

            [FeatureConfigOption("Raids", "", 6)]
            public bool JoinRaids = false;

            [FeatureConfigOption("High End Duty", "", 7)]
            public bool JoinHighEndDuty = false;

            [FeatureConfigOption("PvP", "", 8)]
            public bool JoinPvP = false;

            [FeatureConfigOption("Quest Battles", "", 9)]
            public bool JoinQuestBattles = false;

            [FeatureConfigOption("FATEs", "", 10)]
            public bool JoinFATEs = false;

            [FeatureConfigOption("Treasure Hunt", "", 11)]
            public bool JoinTreasureHunt = false;

            [FeatureConfigOption("The Hunt", "", 12)]
            public bool JoinTheHunt = false;

            [FeatureConfigOption("Gathering Forays", "", 13)]
            public bool JoinGatheringForays = false;

            [FeatureConfigOption("Deep Dungeons", "", 14)]
            public bool JoinDeepDungeons = false;

            [FeatureConfigOption("Field Operations", "", 15)]
            public bool JoinFieldOperations = false;

            [FeatureConfigOption("V&C Dungeon Finder", "", 16)]
            public bool JoinVCDungeonFinder = false;
        }

        public readonly struct Categories
        {
            public int IconID { get; }
            public string Name { get; }
            public Func<bool> GetConfigValue { get; }

            public Categories(int iconID, string name, Func<bool> configValue)
            {
                IconID = iconID;
                Name = name;
                GetConfigValue = configValue;
            }
        }

        private readonly Categories[] categories;

        public AutoJoinPF()
        {
            categories = new Categories[]
            {
                new Categories(61699, "None", () => Config.JoinNone),
                new Categories(61801, "Dungeons", () => Config.JoinDungeons),
                new Categories(61802, "Raids", () => Config.JoinRaids),
                new Categories(61803, "Guildhests", () => Config.JoinGuildhests),
                new Categories(61804, "Trials", () => Config.JoinTrials),
                new Categories(61805, "Quest Battles", () => Config.JoinQuestBattles),
                new Categories(61806, "PvP", () => Config.JoinPvP),
                new Categories(61807, "Duty Roulette", () => Config.JoinDutyRoulette),
                new Categories(61808, "Treasure Hunt", () => Config.JoinTreasureHunt),
                new Categories(61809, "FATEs", () => Config.JoinFATEs),
                new Categories(61815, "Gathering Forays", () => Config.JoinGatheringForays),
                new Categories(61819, "The Hunt", () => Config.JoinTheHunt),
                new Categories(61824, "Deep Dungeons", () => Config.JoinDeepDungeons),
                new Categories(61832, "High End Duty", () => Config.JoinHighEndDuty),
                new Categories(61837, "Field Operations", () => Config.JoinFieldOperations),
                new Categories(61846, "VC Dungeon Finder", () => Config.JoinVCDungeonFinder),
            };
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Common.OnAddonSetup += RunFeature;
            Common.OnAddonSetup += ConfirmYesNo;
            base.Enable();
        }

        private void RunFeature(SetupAddonArgs obj)
        {
            if (obj.AddonName != "LookingForGroupDetail") return;

            TaskManager.Enqueue(() => new nint(obj.Addon->AtkValues[11].String) != 0);
            TaskManager.Enqueue(() => AutoJoin(obj.Addon));
        }

        private void AutoJoin(AtkUnitBase* addon)
        {
            if (IsPrivatePF(addon) || IsSelfParty(addon) || !CanJoinPartyType(GetPartyType(addon))) return;

            Callback.Fire(addon, false, 0);
        }

        private bool IsPrivatePF(AtkUnitBase* addon) =>
            // 111 is the lock icon
            addon->UldManager.NodeList[111]->IsVisible;

        private bool IsSelfParty(AtkUnitBase* addon) =>
            MemoryHelper.ReadSeStringNullTerminated(new nint(addon->AtkValues[11].String)).ToString() == Svc.ClientState.LocalPlayer.Name.TextValue;

        private string GetPartyType(AtkUnitBase* addon) =>
            categories.FirstOrDefault(x => x.IconID == addon->AtkValues[16].Int).Name;

        public bool CanJoinPartyType(string categoryName) => 
            categories.FirstOrDefault(c => c.Name == categoryName).GetConfigValue();

        internal void ConfirmYesNo(SetupAddonArgs obj)
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied39]) return;
            if (obj.AddonName != "SelectYesno") return;

            if (TryGetAddonByName<AtkUnitBase>("LookingForGroupDetail", out var lfgAddon) && lfgAddon->IsVisible)
                if (CanJoinPartyType(GetPartyType(lfgAddon)) && obj.Addon->UldManager.NodeList[15]->IsVisible)
                    new ClickSelectYesNo((IntPtr)obj.Addon).Yes();
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Common.OnAddonSetup -= RunFeature;
            Common.OnAddonSetup -= ConfirmYesNo;
            base.Disable();
        }
    }
}
