using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.JobGauge.Types;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.EzHookManager;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using PandorasBox.FeaturesSetup;
namespace PandorasBox.Features.Actions;

internal class AutoMeditation : Feature
{
    public override string Name => "Auto-Meditation";

    public override string Description => "Automatically use Meditation when out of combat.";

    public override FeatureType FeatureType => FeatureType.Actions;

    public override void Enable()
    {
        Svc.Framework.Update += RunFeature;
        Events.OnJobChanged += DelayStart;
        if (SendActionHook is null) EzSignatureHelper.Initialize(this);
        else SendActionHook?.Enable();
        base.Enable();
    }

    private static void DelayStart(uint? jobId)
    {
        EzThrottler.Throttle("MNKMed", 3000);
    }

    private static unsafe void RunFeature(IFramework framework)
    {
        if (Player.Object is null) return;
        var isMonk = Player.Job == Job.MNK;
        var isPugilist = Player.Job == Job.PGL;
        if (!isMonk && !isPugilist) return;
        var gauge = Svc.Gauges.Get<MNKGauge>();
        if (gauge.Chakra == 5) return;
        if (Svc.Condition[ConditionFlag.InCombat]) return;
        if (TerritoryInfo.Instance()->InSanctuary) return;

        if (!Svc.Condition[ConditionFlag.InCombat] && EzThrottler.Throttle("PCTMotifs", 1500))
        {
            var am = ActionManager.Instance();
            if (am->GetActionStatus(ActionType.Action, 36942) == 0)
                am->UseAction(ActionType.Action, 36942);
            if (am->GetActionStatus(ActionType.Action, 36940) == 0)
                am->UseAction(ActionType.Action, 36940);
        }
    }

    public override void Disable()
    {
        Svc.Framework.Update -= RunFeature;
        Events.OnJobChanged -= DelayStart;
        SendActionHook?.Disable();
        base.Disable();
    }
}
