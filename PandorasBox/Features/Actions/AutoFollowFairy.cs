using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using PandorasBox.FeaturesSetup;
using System;
using System.Collections.Generic;
using System.Text;

namespace PandorasBox.Features.Actions;

internal class AutoFollowFairy : Feature {
    public override string Name => "Auto-Follow Fairy";

    public override string Description => "After combat, automatically call your fairy to follow you. Useful if you've placed it manually.";

    public override FeatureType FeatureType => FeatureType.Actions;

    public override void Enable() {
        base.Enable();
        Svc.Condition.ConditionChange += Condition_ConditionChange;
    }

    public override void Disable() {
        base.Disable();
        Svc.Condition.ConditionChange -= Condition_ConditionChange;
    }

    private unsafe void Condition_ConditionChange(ConditionFlag flag, bool value) {
        if (flag == ConditionFlag.InCombat && !value) {
            var am = FFXIVClientStructs.FFXIV.Client.Game.ActionManager.Instance();
            if (am->GetActionStatus(FFXIVClientStructs.FFXIV.Client.Game.ActionType.PetAction, 2) != 0) return;
            am->UseAction(FFXIVClientStructs.FFXIV.Client.Game.ActionType.PetAction, 2);
        }
    }
}
