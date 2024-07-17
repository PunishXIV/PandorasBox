using Dalamud.Hooking;
using ECommons.DalamudServices;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using PandorasBox.FeaturesSetup;
using System;

namespace PandorasBox.Features.Actions
{
    internal class AutoMotif : Feature
    {
        public override string Name => "Auto-Motif (Out of Combat)";
        public override string Description => "Automatically draws motifs outside of combat if not already drawn.";
        public override FeatureType FeatureType => FeatureType.Actions;

        private delegate void SendActionDelegate(ulong targetObjectId, byte actionType, uint actionId, ushort sequence, long a5, long a6, long a7, long a8, long a9);
        private static Hook<SendActionDelegate>? SendActionHook;

        public override void Enable()
        {
            Svc.Framework.Update += CheckMotifs;
            SendActionHook ??= Svc.Hook.HookFromSignature<SendActionDelegate>("48 89 5C 24 ?? 48 89 6C 24 ?? 48 89 74 24 ?? 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8B E9 41 0F B7 D9", SendActionDetour);
            SendActionHook?.Enable();
            base.Enable();
        }

        private void SendActionDetour(ulong targetObjectId, byte actionType, uint actionId, ushort sequence, long a5, long a6, long a7, long a8, long a9)
        {
            EzThrottler.Reset("PCTMotifs");
            SendActionHook?.Original(targetObjectId, actionType, actionId, sequence, a5, a6, a7, a8, a9);   
        }

        private unsafe void CheckMotifs(IFramework framework)
        {
            if (Svc.ClientState.LocalPlayer is null) return;
            if (Svc.ClientState.LocalPlayer.ClassJob.Id != 42) return;
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat]) return;

            if (EzThrottler.Throttle("PCTMotifs", 1500))
            {
                var am = ActionManager.Instance();
                if (am->GetActionStatus(ActionType.Action, 34664) == 0)
                    am->UseAction(ActionType.Action, 34664);
                if (am->GetActionStatus(ActionType.Action, 34665) == 0)
                    am->UseAction(ActionType.Action, 34665);
                if (am->GetActionStatus(ActionType.Action, 34666) == 0)
                    am->UseAction(ActionType.Action, 34666);
                if (am->GetActionStatus(ActionType.Action, 34667) == 0)
                    am->UseAction(ActionType.Action, 34667);
                if (am->GetActionStatus(ActionType.Action, 34668) == 0)
                    am->UseAction(ActionType.Action, 34668);
                if (am->GetActionStatus(ActionType.Action, 34669) == 0)
                    am->UseAction(ActionType.Action, 34669);
            }
        }

        public override void Disable()
        {
            Svc.Framework.Update -= CheckMotifs;
            SendActionHook?.Disable();
            SendActionHook?.Dispose();
            base.Disable();
        }
    }
}
