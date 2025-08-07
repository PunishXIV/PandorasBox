using Dalamud.Game.ClientState.Conditions;
using ECommons;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using System.Collections.Generic;
using System.Linq;

namespace PandorasBox.Features.Targets
{
    public unsafe class AutoInteractDungeons : Feature
    {
        public override string Name => "Auto-interact with Objects in Instances";

        public override string Description => "Automatically try to pick all the keys, levers, and other thingymabobs. Also works to try and open doors and stuff. (VERY EXPERIMENTAL)";

        public override FeatureType FeatureType => FeatureType.Targeting;

        public List<uint> Exits = new() { 2000046, 2000066, 2000139, 2000187, 2000275, 2000370, 2000493, 2000596, 2000605, 2000683, 2000788,
                                                     2001143, 2001144, 2001215, 2001216, 2001610, 2001695, 2001716, 2001717, 2001835, 2001871, 2002502,
                                                     2002738, 2002740, 2002879, 2002880, 2002888, 2003454, 2004012, 2004013, 2004014, 2004015, 2004361,
                                                     2004651, 2004781, 2004783, 2004805, 2004936, 2004966, 2005218, 2005313, 2005332, 2005333, 2005334,
                                                     2005335, 2005337, 2005445, 2005809, 2006016, 2006235, 2006413, 2006421, 2006430, 2006431, 2006502,
                                                     2006963, 2007002, 2007003, 2007076, 2007240, 2007380, 2007403, 2007444, 2007463, 2007465, 2007515,
                                                     2007526, 2007528, 2007530, 2007766, 2007781, 2007786, 2008134, 2008372, 2008489, 2009037, 2009044,
                                                     2009071, 2009215, 2009267, 2009289, 2009468, 2009523, 2009641, 2009658, 2009675, 2009751, 2009753,
                                                     2009758, 2009986, 2009987, 2010599, 2010600, 2010601, 2010602, 2010635, 2010742, 2010759, 2010829,
                                                     2010831, 2010954, 2011059, 2011084, 2011102, 2011105, 2011137, 2011155, 2011235, 2011236, 2011237,
                                                     2011238, 2011239, 2011250, 2011268, 2011277, 2011282, 2011309, 2011393, 2011583, 2011673, 2011721,
                                                     2011729, 2011739, 2011851, 2011937, 2012292, 2012294, 2012296, 2012341, 2012380, 2012385, 2012528,
                                                     2012529, 2012531, 2012533, 2012613, 2012683, 2012718, 2012721, 2012842, 2012871, 2013077, 2013104,
                                                     2013137, 2013167, 2013227, 2013290 };

        public class Configs : FeatureConfig
        {
            public float ThrottleF = 0.1f;

            public float Cooldown = 0.1f;

            public float MaxDistance = 0.5f;

            public float MaxHeight = 0.1f;

            public bool ExcludeExit = false;

            public bool ExcludeCombat = false;

            public bool OnlyStanding = false;

            public int InteractMethod = 1;
        }


        public Configs Config { get; private set; }
        public void TryInteract(GameObject* baseObj)
        {
            if (Config.InteractMethod is 1 or 3)
            {
                if (baseObj->GetIsTargetable())
                    TargetSystem.Instance()->InteractWithObject(baseObj, true);
            }

            if (Config.InteractMethod is 2 or 3)
                TaskManager.EnqueueWithTimeout(() => { if (!Svc.Condition[ConditionFlag.OccupiedInQuestEvent]) TargetSystem.Instance()->OpenObjectInteraction(baseObj); }, 100);
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Framework.Update += RunFeature;
            Svc.Condition.ConditionChange += TriggerCooldown;
            base.Enable();
        }

        private void TriggerCooldown(ConditionFlag flag, bool value)
        {
            if ((flag == ConditionFlag.OccupiedInQuestEvent) && !value)
                TaskManager.EnqueueDelay((int)(Config.Cooldown * 1000));
        }

        private void RunFeature(IFramework framework)
        {
            if (Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.CarryingItem] || Svc.Condition[ConditionFlag.CarryingObject])
            {
                TaskManager.Abort();
                return;
            }
            if (Svc.ClientState.LocalPlayer == null) return;

            if (GameMain.Instance()->CurrentContentFinderConditionId > 0)
            {
                if (Svc.Condition[ConditionFlag.InCombat] && Config.ExcludeCombat) { TaskManager.Abort(); return; }
                if (IsMoving() && Config.OnlyStanding) { TaskManager.Abort(); return; }

                var nearbyNodes = Svc.Objects.Where(x => x.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.EventObj &&
                                                    GameObjectHelper.GetTargetDistance(x) <= Config.MaxDistance &&
                                                    GameObjectHelper.GetHeightDifference(x) <= Config.MaxHeight &&
                                                    ((GameObject*)x.Address)->RenderFlags == 0).ToList();
                if (nearbyNodes.Count == 0)
                    return;

                if (Svc.Targets.Target != null)
                {
                    if (nearbyNodes.Any(x => x.DataId == Svc.Targets.Target.DataId))
                    {
                        var nearestNode = nearbyNodes.First(x => x.DataId == Svc.Targets.Target.DataId);
                        var baseObj = (GameObject*)nearestNode.Address;

                        if (!TargetSystem.Instance()->IsObjectInViewRange(baseObj) || !TargetSystem.Instance()->IsObjectOnScreen(baseObj)) return;

                        TaskManager.EnqueueDelay((int)(Config.ThrottleF * 1000));
                        TaskManager.Enqueue(() => TryInteract(baseObj));
                        return;
                    }
                }

                foreach (var nearestNode in nearbyNodes.OrderBy(GameObjectHelper.GetTargetDistance))
                {
                    var baseObj = (GameObject*)nearestNode.Address;
                    if (baseObj->RenderFlags != 0) continue;
                    if (string.IsNullOrEmpty(baseObj->NameString)) continue;
                    if (!TargetSystem.Instance()->IsObjectInViewRange(baseObj)) continue;

                    if (Svc.Data.GetExcelSheet<EObj>().TryGetFirst(x => x.RowId == baseObj->BaseId, out var sheetItem))
                        if (Config.ExcludeExit && (sheetItem.SgbPath.ValueNullable?.SgbPath.ToString().Contains("bgcommon/world/lvd/shared/for_vfx/sgvf_w_lvd_b0005.sgb") == true || Exits.Contains(sheetItem.RowId)))
                            continue;

                    if (!TaskManager.IsBusy)
                    {
                        if (Svc.Condition[ConditionFlag.OccupiedInQuestEvent]) continue;
                        TaskManager.EnqueueDelay((int)(Config.ThrottleF * 1000));
                        TaskManager.Enqueue(() => TryInteract(baseObj));
                    }

                }

            }
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Svc.Framework.Update -= RunFeature;
            Svc.Condition.ConditionChange -= TriggerCooldown;
            base.Disable();
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
        {
            var defaultAttr = new FeatureConfigOptionAttribute("");
            ImGui.PushItemWidth(300);
            if (ImGui.SliderFloat($"Set delay (seconds)", ref Config.ThrottleF, 0.1f, 10f, defaultAttr.Format)) hasChanged = true;
            if (ImGui.SliderFloat($"Cooldown after interacting (seconds)", ref Config.Cooldown, 0.1f, 10f, defaultAttr.Format)) hasChanged = true;
            if (ImGui.SliderFloat($"Max Distance (yalms)", ref Config.MaxDistance, 0.5f, 5f, defaultAttr.Format)) hasChanged = true;
            if (ImGui.SliderFloat($"Max Height Difference (yalms)", ref Config.MaxHeight, 0.1f, 10f, defaultAttr.Format)) hasChanged = true;
            if (ImGui.Checkbox($"Exclude Combat", ref Config.ExcludeCombat)) hasChanged = true;
            if (ImGui.Checkbox($"Exclude Exits", ref Config.ExcludeExit)) hasChanged = true;
            if (ImGui.Checkbox($"Only Attempt Whilst Not Moving", ref Config.OnlyStanding)) hasChanged = true;

            if (ImGui.RadioButton($"Try Interact Method 1 (interacts with most things)", Config.InteractMethod == 1))
            {
                Config.InteractMethod = 1;
                hasChanged = true;
            }
            if (ImGui.RadioButton($"Try Interact Method 2 (interacts with things method 1 doesn't)", Config.InteractMethod == 2))
            {
                Config.InteractMethod = 2;
                hasChanged = true;
            }
            if (ImGui.RadioButton($"Try Interact with Both Methods (buggy on many things, use with caution)", Config.InteractMethod == 3))
            {
                Config.InteractMethod = 3;
                hasChanged = true;
            }

        };
    }
}
