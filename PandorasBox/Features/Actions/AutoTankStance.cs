using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
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

        public List<uint> Stances { get; set; } = new List<uint>() { 79, 91, 743, 1833 };

        public uint MainTank = 0;

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

        public Configs? Config { get; private set; }

        public override bool UseAutoConfig => true;


        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Events.OnJobChanged += RunFeature;
            Svc.ClientState.TerritoryChanged += CheckIfDungeon;
            Svc.Framework.Update += CheckParty;
            Svc.Framework.Update += CheckForFateSync;
            base.Enable();
        }

        private void CheckParty(IFramework framework)
        {
            if (Svc.Party.Length == 0 || Svc.Party.Any(x => x == null) || Svc.ClientState.LocalPlayer == null || Svc.Condition[ConditionFlag.BetweenAreas]) return;
            if (Config!.ActivateOnDeath && Svc.Party.Any(x => x != null && x.ObjectId != Svc.ClientState.LocalPlayer?.GameObjectId && x.Statuses.Any(y => Stances.Any(z => y.StatusId == z))))
            {
                MainTank = Svc.Party.First(x => x != null && x.ObjectId != Svc.ClientState.LocalPlayer.GameObjectId && x.Statuses.Any(y => Stances.Any(z => y.StatusId == z))).ObjectId;
            }
            else
            {
                MainTank = 0;
            }

            if (Svc.Party.Any(x => x.ObjectId == MainTank))
            {
                if (MainTank != 0 && Svc.Party.First(x => x.ObjectId == MainTank).GameObject!.IsDead && !Svc.ClientState.LocalPlayer.StatusList.Any(x => Stances.Any(y => x.StatusId == y)))
                {
                    EnableStance();
                    TaskManager!.Enqueue(() => TaskManager.Abort());
                }
            }
        }

        private void CheckIfDungeon(ushort e)
        {
            if (GameMain.Instance()->CurrentContentFinderConditionId == 0)
            {
                TaskManager!.Abort();
                return;
            }
            TaskManager!.Enqueue(() => Player.Available);
            TaskManager!.Enqueue(() => Svc.DutyState.IsDutyStarted);
            TaskManager!.Enqueue(() => EnableStance(), "TankStanceDungeonEnabled");

        }

        private void CheckForFateSync(IFramework framework)
        {
            if (HasStance()) return;
            var ps = PlayerState.Instance();
            if (Config!.ActivateInFate && FateManager.Instance()->CurrentFate != null)
            {
                TaskManager!.Enqueue(() => EnableStance(), "FateSync");
            }
        }

        private void RunFeature(uint? jobId)
        {
            if (HasStance()) return;
            EnableStance();
        }

        private bool HasStance()
        {
            if (!Player.Available) return false;
            ushort stance = Svc.ClientState.LocalPlayer?.ClassJob.RowId switch
            {
                1 or 19 => 79,
                3 or 21 => 91,
                32 => 743,
                37 => 1833,
                _ => 0
            };

            if (stance == 0) return true;
            if (Svc.ClientState.LocalPlayer!.StatusList.Any(x => x.StatusId == stance)) return true;
            return false;
        }
        private bool EnableStance()
        {
            if (Svc.ClientState.LocalPlayer?.GetRole() is not CombatRole.Tank) return true;
            if (Config!.OnlyInDuty && !IsInDuty()) return true;

            var am = ActionManager.Instance();
            if (Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent]) return false;
            TaskManager!.EnqueueDelay((int)(Config.Throttle * 1000));
            TaskManager.Enqueue(() =>
            {
                if (Svc.Party.Length > Config.MaxParty) return true;
                if (Config.NoOtherTanks && Svc.Party.Any(x => x.ObjectId != Svc.ClientState.LocalPlayer!.GameObjectId && x.Statuses.Any(y => Stances.Any(z => y.StatusId == z)))) return true;

                uint action = Svc.ClientState.LocalPlayer!.ClassJob.RowId switch
                {
                    1 or 19 => 28,
                    3 or 21 => 48,
                    32 => 3629,
                    37 => 16142,
                    _ => throw new System.NotImplementedException()
                };

                if (HasStance()) return true;

                if (am->GetActionStatus(ActionType.Action, action) == 0)
                {
                    am->UseAction(ActionType.Action, action);
                    return true;
                }

                return false;
            });


            return true;
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Events.OnJobChanged -= RunFeature;
            Svc.ClientState.TerritoryChanged -= CheckIfDungeon;
            Svc.Framework.Update -= CheckParty;
            Svc.Framework.Update -= CheckForFateSync;
            base.Disable();
        }
    }
}
