using ECommons.DalamudServices;
using ECommons.EzHookManager;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using PandorasBox.FeaturesSetup;

namespace PandorasBox.Features.Actions
{
    internal class AutoMotif : Feature
    {
        public override string Name => "Auto-Motif (Out of Combat)";
        public override string Description => "Automatically draws motifs when outside of combat and not in a sanctuary.";
        public override FeatureType FeatureType => FeatureType.Actions;

        public override void Enable()
        {
            Svc.Framework.Update += CheckMotifs;
            Events.OnJobChanged += DelayStart;
            if (SendActionHook is null) EzSignatureHelper.Initialize(this);
            else SendActionHook?.Enable();
            base.Enable();
        }

        private void DelayStart(uint? jobId)
        {
            EzThrottler.Throttle("PCTMotifs", 3000);
        }

        public override void SendActionDetour(ulong targetObjectId, byte actionType, uint actionId, ushort sequence, long a5, long a6, long a7, long a8, long a9)
        {
            EzThrottler.Reset("PCTMotifs");
            base.SendActionDetour(targetObjectId, actionType, actionId, sequence, a5, a6, a7, a8, a9);
        }

        private unsafe void CheckMotifs(IFramework framework)
        {
            if (Svc.ClientState.LocalPlayer is null) return;
            if (Svc.ClientState.LocalPlayer.ClassJob.RowId != 42) return;
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat]) return;
            if (TerritoryInfo.Instance()->InSanctuary) return;

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
            Events.OnJobChanged -= DelayStart;
            SendActionHook?.Disable();
            base.Disable();
        }
    }
}
