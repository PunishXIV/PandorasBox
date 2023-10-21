using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Fate;
using PandorasBox.FeaturesSetup;
using System.Collections.Generic;
using System.Linq;
using PlayerState = FFXIVClientStructs.FFXIV.Client.Game.UI.PlayerState;

namespace PandorasBox.Features.Actions
{
    public unsafe class AutoTankStance : Feature
    {
        public override string Name => "Auto-Tank Stance";
        public override string Description => "Activates your tank stance automatically upon job switching or entering a dungeon.";
        public override FeatureType FeatureType => FeatureType.Actions;

        public Configs Config { get; private set; }
        public override bool UseAutoConfig => true;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Set delay (seconds)", FloatMin = 0.1f, FloatMax = 10f, EditorSize = 300, EnforcedLimit = true)]
            public float Throttle = 0.1f;

            [FeatureConfigOption("Activate when party size is less than or equal to", IntMin = 1, IntMax = 8, EditorSize = 300)]
            public int MaxParty = 1;

            [FeatureConfigOption("Function only in a duty", "", 1)]
            public bool OnlyInDuty = false;

            [FeatureConfigOption("Activate if main tank dies (respects party size option)", "", 2)]
            public bool ActivateOnDeath = false;

            [FeatureConfigOption("Only activate on entrance if no other tank has stance", "", 3)]
            public bool NoOtherTanks = false;

            [FeatureConfigOption("Activate when synced to a fate", "", 4)]
            public bool ActivateInFate = false;
        }

        public List<uint> Stances { get; set; } = new List<uint>() { 79, 91, 743, 1833 };
        public uint MainTank = 0;

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            OnJobChanged += RunFeature;
            Svc.ClientState.TerritoryChanged += CheckIfDungeon;
            Svc.Framework.Update += CheckParty;
            Svc.Framework.Update += CheckForFateSync;
            base.Enable();
        }

        public override void Disable()
        {
            SaveConfig(Config);
            OnJobChanged -= RunFeature;
            Svc.ClientState.TerritoryChanged -= CheckIfDungeon;
            Svc.Framework.Update -= CheckParty;
            Svc.Framework.Update -= CheckForFateSync;
            base.Disable();
        }

        private void RunFeature(uint? jobId)
        {
            if (Svc.ClientState.LocalPlayer.ClassJob.GameData.Role == 1)
                EnableStance();
        }

        private void CheckIfDungeon(ushort e)
        {
            if (GameMain.Instance()->CurrentContentFinderConditionId == 0) return;
            TaskManager.Enqueue(() => Svc.ClientState.LocalPlayer != null);
            TaskManager.DelayNext("TankWaitForConditions", 2000);
            TaskManager.Enqueue(() => Svc.DutyState.IsDutyStarted);
            TaskManager.Enqueue(() => EnableStance(), "TankStanceDungeonEnabled");
        }

        private void CheckParty(IFramework framework)
        {
            if (Svc.Party.Length == 0 || Svc.Party.Any(x => x == null) || Svc.ClientState.LocalPlayer == null || Svc.Condition[ConditionFlag.BetweenAreas]) return;

            if (Config.ActivateOnDeath && Svc.Party.Any(x => x != null && x.ObjectId != Svc.ClientState.LocalPlayer?.ObjectId && x.Statuses.Any(y => Stances.Any(z => y.StatusId == z))))
            {
                MainTank = Svc.Party.First(x => x != null && x.ObjectId != Svc.ClientState.LocalPlayer.ObjectId && x.Statuses.Any(y => Stances.Any(z => y.StatusId == z))).ObjectId;
            }
            else
            {
                MainTank = 0;
            }

            if (Svc.Party.Any(x => x.ObjectId == MainTank))
            {
                if (MainTank != 0 && Svc.Party.First(x => x.ObjectId == MainTank).GameObject.IsDead && !Svc.ClientState.LocalPlayer.StatusList.Any(x => Stances.Any(y => x.StatusId == y)))
                {
                    EnableStance();
                    TaskManager.Enqueue(() => TaskManager.Abort());
                }
            }
        }

        private void CheckForFateSync(IFramework framework)
        {
            var ps = PlayerState.Instance();
            if (Config.ActivateInFate && FateManager.Instance()->CurrentFate != null && ps->IsLevelSynced == 1)
                TaskManager.Enqueue(() => EnableStance());
        }

        private bool EnableStance()
        {
            if (Svc.ClientState.LocalPlayer.ClassJob.GameData.Role != 1) return true;
            if (Config.OnlyInDuty && !IsInDuty()) return true;

            var am = ActionManager.Instance();
            if (Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent]) return false;
            TaskManager.DelayNext("TankStance", (int)(Config.Throttle * 1000));
            TaskManager.Enqueue(() =>
            {
                if (Svc.Party.Length > Config.MaxParty) return true;
                if (Config.NoOtherTanks && Svc.Party.Any(x => x.ObjectId != Svc.ClientState.LocalPlayer.ObjectId && x.Statuses.Any(y => Stances.Any(z => y.StatusId == z)))) return true;

                uint action = Svc.ClientState.LocalPlayer.ClassJob.Id switch
                {
                    1 or 19 => 28,
                    3 or 21 => 48,
                    32 => 3629,
                    37 => 16142
                };

                ushort stance = Svc.ClientState.LocalPlayer.ClassJob.Id switch
                {
                    1 or 19 => 79,
                    3 or 21 => 91,
                    32 => 743,
                    37 => 1833
                };

                if (Svc.ClientState.LocalPlayer.StatusList.Any(x => x.StatusId == stance)) return true;
                if (am->GetActionStatus(ActionType.Action, action) == 0)
                {
                    am->UseAction(ActionType.Action, action);
                    return true;
                }

                return false;
            });

            return true;
        }
    }
}
