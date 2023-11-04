//using Dalamud.Game.ClientState.Conditions;
//using ECommons.Automation;
//using ECommons.DalamudServices;
//using ECommons.GameHelpers;
//using FFXIVClientStructs.FFXIV.Client.Game;
//using FFXIVClientStructs.FFXIV.Client.Game.MJI;
//using PandorasBox.FeaturesSetup;
//using System;
//using System.Linq;
//using System.Numerics;

//namespace PandorasBox.Features.Actions
//{
//    internal class SanctuaryLock_Move : Feature
//    {
//        public override string Name => "Island Sanctuary Auto-Lock & Move";

//        public override string Description => "After gathering from an island sanctuary node, try to auto-lock onto the nearest gatherable and walk towards it.";

//        public override FeatureType FeatureType => FeatureType.Disabled;

//        private bool LockingOn = false;
//        public override void Enable()
//        {
//            Svc.Framework.Update += CheckToJump;
//            Svc.Condition.ConditionChange += CheckToLockAndMove;
//            base.Enable();
//        }

//        private unsafe void CheckToJump(IFramework framework)
//        {
//            if (Svc.Targets.Target == null || Svc.Targets.Target.ObjectKind != Dalamud.Game.ClientState.Objects.Enums.ObjectKind.CardStand)
//                return;

//            if (IsMoving() && IsTargetLocked && MJIManager.Instance()->IsPlayerInSanctuary != 0 && ActionManager.Instance()->GetActionStatus(ActionType.GeneralAction, 2) == 0 && Vector3.Distance(Svc.Targets.Target.Position, Player.Object.Position) > 8)
//            {
//                if (!TaskManager.IsBusy)
//                {
//                    TaskManager.DelayNext(new Random().Next(300, 550));
//                    TaskManager.Enqueue(() => ActionManager.Instance()->UseAction(ActionType.GeneralAction, 2));
//                }
//            }
//        }

//        private unsafe void CheckToLockAndMove(ConditionFlag flag, bool value)
//        {
//            if (flag == ConditionFlag.OccupiedInQuestEvent && !value && MJIManager.Instance()->IsPlayerInSanctuary == 1)
//            {
//                if (Svc.ClientState.LocalPlayer is null) return;
//                if (Svc.ClientState.LocalPlayer.IsCasting) return;

//                TaskManager.DelayNext(300);
//                TaskManager.Enqueue(() =>
//                {
//                    var nearbyNodes = Svc.Objects.Where(x => x.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.CardStand && x.IsTargetable).ToList();
//                    if (nearbyNodes.Count == 0)
//                        return;

//                    var nearestNode = nearbyNodes.OrderBy(x => Vector3.Distance(x.Position, Player.Object.Position)).FirstOrDefault();
//                    if (nearestNode != null && nearestNode.IsTargetable)
//                    {
//                        Svc.Targets.Target = nearestNode;
//                    }

//                    if (MJIManager.Instance()->CurrentMode == 1)
//                    {
//                        TaskManager.Enqueue(() => { LockingOn = true; Chat.Instance.SendMessage("/lockon on"); });
//                        TaskManager.DelayNext(new Random().Next(100, 250));
//                        TaskManager.Enqueue(() => { if (IsTargetLocked) { Chat.Instance.SendMessage("/automove on"); LockingOn = false; } });

//                    }
//                });
//            }
//        }

//        public override void Disable()
//        {
//            Svc.Framework.Update -= CheckToJump;
//            Svc.Condition.ConditionChange -= CheckToLockAndMove;
//            base.Disable();
//        }
//    }
//}
