using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using System.Linq;

namespace PandorasBox.Features.Other
{
    public unsafe class AutoSyncFate : Feature
    {
        private ushort fateID;

        public override string Name => "Auto-Sync FATEs";

        public override string Description => "Syncs when entering a FATE if you're overlevelled.";

        public override FeatureType FeatureType => FeatureType.Other;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption($@"Exclude ""A Realm Reborn"" zones", "" , 1)]
            public bool ExcludeARR = false;

            [FeatureConfigOption($@"Exclude ""Heavensward"" zones", "", 2)]
            public bool ExcludeHW = false;

            [FeatureConfigOption($@"Exclude ""Stormblood"" zones", "", 3)]
            public bool ExcludeSB = false;

            [FeatureConfigOption($@"Exclude ""Shadowbringers"" zones", "", 4)]
            public bool ExcludeShB = false;

            [FeatureConfigOption($@"Exclude ""Endwalker"" zones", "", 5)]
            public bool ExcludeEW = false;

            [FeatureConfigOption("Don't trigger when in combat", "", 6)]
            public bool ExcludeCombat = false;
        }

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => true;

        public ushort FateID
        {
            get => fateID; set
            {
                if (fateID != value)
                {
                    SyncFate(value);
                }
                fateID = value;
            }
        }

        public byte FateMaxLevel;
        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Framework.Update += CheckFates;
            base.Enable();
        }

        public void SyncFate(ushort value)
        {
            if (value != 0)
            {
                var zone = Svc.Data.GetExcelSheet<TerritoryType>().Where(x => x.RowId == Svc.ClientState.TerritoryType).First();
                if (zone.ExVersion.Row == 0 && Config.ExcludeARR) return;
                if (zone.ExVersion.Row == 1 && Config.ExcludeHW) return;
                if (zone.ExVersion.Row == 2 && Config.ExcludeSB) return;
                if (zone.ExVersion.Row == 3 && Config.ExcludeShB) return;
                if (zone.ExVersion.Row == 4 && Config.ExcludeEW) return;
                if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat] && Config.ExcludeCombat) return;

                if (Svc.ClientState.LocalPlayer.Level > FateMaxLevel)
                Chat.Instance.SendMessage("/lsync");
            }
        }
        private void CheckFates(IFramework framework)
        {
            if (FateManager.Instance()->CurrentFate != null)
            {
                FateMaxLevel = FateManager.Instance()->CurrentFate->MaxLevel;
                FateID = FateManager.Instance()->CurrentFate->FateId;
              
            }
            else
            {
                FateID = 0;
            }
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Svc.Framework.Update -= CheckFates;
            base.Disable();
        }
    }
}
