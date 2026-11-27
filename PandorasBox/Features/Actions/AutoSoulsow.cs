using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.EzHookManager;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using PandorasBox.FeaturesSetup;

namespace PandorasBox.Features.Actions;

internal class AutoSoulsow : Feature {
    public override string Name => "Auto-Soulsow (Out of Combat)";
    public override string Description => "Automatically uses Soulsow when outside of combat and not in a sanctuary.";
    public override FeatureType FeatureType => FeatureType.Actions;

    public override void Enable() {
        Svc.Framework.Update += RunFeature;
        Events.OnJobChanged += DelayStart;
        if (SendActionHook is null) EzSignatureHelper.Initialize(this);
        else SendActionHook?.Enable();
        base.Enable();
    }

    private static void DelayStart(uint? jobId) {
        EzThrottler.Throttle("RPRSoulsow", 1500);
    }

    private static unsafe void RunFeature(IFramework framework) {
        if (Player.Object is null) return;
        var isReaper = Player.Job == Job.RPR;
        if (!isReaper) return;
        if (Svc.Condition[ConditionFlag.InCombat]) return;
        if (TerritoryInfo.Instance()->InSanctuary) return;

        if (!Svc.Condition[ConditionFlag.InCombat] && EzThrottler.Throttle("RPRSoulsow", 1500)) {
            var am = ActionManager.Instance();
            if ((am->GetActionStatus(ActionType.Action, 24388) != 0) && am->GetActionStatus(ActionType.Action, 24387) == 0)
                am->UseAction(ActionType.Action, 24387);
        }
    }

    public override void Disable() {
        Svc.Framework.Update -= RunFeature;
        Events.OnJobChanged -= DelayStart;
        SendActionHook?.Disable();
        base.Disable();
    }
}
