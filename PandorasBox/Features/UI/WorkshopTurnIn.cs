using ClickLib.Clicks;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Interface;
using Dalamud.Memory;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Interop;
using ECommons.Logging;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Common.Math;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using PandorasBox.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using static ECommons.GenericHelpers;

namespace PandorasBox.Features.UI
{
    public unsafe class WorkshopTurnin : Feature
    {
        public override string Name => "FC Workshop Hand-In";

        public override string Description => "Adds buttons to auto hand-in the current phase and the entire project to the workshop menu.";

        public override FeatureType FeatureType => FeatureType.UI;

        private Overlays overlay;
        private float height;

        internal bool active = false;
        internal bool phaseActive = false;
        internal bool projectActive = false;
        internal bool partLoopActive = false;
        private static readonly string[] SkipCutsceneStr = { "Skip cutscene?", "要跳过这段过场动画吗？", "要跳過這段過場動畫嗎？", "Videosequenz überspringen?", "Passer la scène cinématique ?", "このカットシーンをスキップしますか？" };
        internal static string[] PanelName = new string[] { "Fabrication Station" };

        public Configs Config { get; private set; }
        public override bool UseAutoConfig => false;
        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Times to loop", "", 1, IntMin = 0, IntMax = 100, EditorSize = 300)]
            public int partsToBuild = 1;
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            overlay = new Overlays(this);
            Common.OnAddonSetup += AutoPhase;
            base.Enable();
        }

        public override void Disable()
        {
            SaveConfig(Config);
            P.Ws.RemoveWindow(overlay);
            Common.OnAddonSetup -= AutoPhase;
            base.Disable();
        }

        public override void Draw()
        {
            if (TryGetAddonByName<AtkUnitBase>("SubmarinePartsMenu", out var addon))
            {
                if (!(addon->UldManager.NodeListCount > 1)) return;
                if (addon->UldManager.NodeListCount < 38) return;
                if (!addon->UldManager.NodeList[1]->IsVisible) return;

                var node = addon->UldManager.NodeList[1];

                if (!node->IsVisible)
                    return;

                var position = AtkResNodeHelper.GetNodePosition(node);

                ImGuiHelpers.ForceNextWindowMainViewport();
                ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(addon->X, addon->Y - height));

                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(7f, 7f));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(10f, 10f));
                ImGui.Begin($"###LoopMelding{node->NodeID}", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoNavFocus
                | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings);

                if (active && !phaseActive) ImGui.BeginDisabled();

                if (ImGui.Button(!phaseActive ? $"Phase Turn In###StartPhaseLooping" : $"Turning in... Click to Abort###AbortPhaseLoop"))
                {
                    if (!phaseActive)
                    {
                        phaseActive = true;
                        TaskManager.Enqueue(YesAlready.DisableIfNeeded);
                        TaskManager.Enqueue(() => TurnInPhase());
                        TaskManager.Enqueue(() => EndLoop("Finished Task"));
                    }
                    else
                    {
                        EndLoop("User cancelled");
                    }
                }

                if (active && !phaseActive) ImGui.EndDisabled();

                //ImGui.SameLine();

                //if (active && !projectActive) ImGui.BeginDisabled();

                //if (ImGui.Button(!projectActive ? $"Project Turn In###StartProjectLooping" : $"Turning in... Click to Abort###AbortProjectLoop"))
                //{
                //    if (!projectActive)
                //    {
                //        projectActive = true;
                //        TaskManager.Enqueue(YesAlready.DisableIfNeeded);
                //        TaskManager.Enqueue(TurnInProject);
                //        TaskManager.Enqueue(() => EndLoop("Finished Task"));
                //    }
                //    else
                //    {
                //        EndLoop("User cancelled");
                //    }
                //}

                //if (active && !projectActive) ImGui.EndDisabled();

                //ImGui.SameLine();

                //if (active && !partLoopActive) ImGui.BeginDisabled();

                //if (ImGui.Button(!partLoopActive ? $"Part Loop Turn In###StartPartLooping" : $"Turning in... Click to Abort###AbortPartLoop"))
                //{
                //    if (!partLoopActive)
                //    {
                //        partLoopActive = true;
                //        TaskManager.Enqueue(YesAlready.DisableIfNeeded);
                //        TaskManager.Enqueue(TurnInProject);
                //        TaskManager.Enqueue(() => EndLoop("Finished Task"));
                //    }
                //    else
                //    {
                //        EndLoop("User cancelled");
                //    }
                //}

                //if (active && !partLoopActive) ImGui.EndDisabled();

                //ImGui.SameLine();

                //if (active) ImGui.BeginDisabled();

                //ImGui.PushItemWidth(225);
                //if (ImGui.SliderInt("Parts to Build", ref Config.partsToBuild, 0, 100))
                //    SaveConfig(Config);

                //if (active) ImGui.EndDisabled();

                active = phaseActive || projectActive || partLoopActive;

                height = ImGui.GetWindowSize().Y;

                ImGui.End();
                ImGui.PopStyleVar(2);
            }
        }

        private bool EndLoop(string msg)
        {
            PluginLog.Log($"Cancelling... Reason: {msg}");
            active = false;
            phaseActive = false;
            projectActive = false;
            partLoopActive = false;
            TaskManager.Abort();
            TaskManager.Enqueue(YesAlready.EnableIfNeeded);
            return true;
        }

        public readonly struct PartIngredient
        {
            public Item Ingredient { get; }

            public uint AmountPerTurnIn { get; }
            public uint TotalRequiredAmount { get; }
            public uint TurnedInSoFar { get; }
            public uint TotalTimesToTurnIn { get; }

            public PartIngredient(Item ingredient, uint perturn, uint total, uint timesSoFar, uint timesTotal)
            {
                Ingredient = ingredient;
                AmountPerTurnIn = perturn;
                TotalRequiredAmount = total;
                TurnedInSoFar = timesSoFar;
                TotalTimesToTurnIn = timesTotal;
            }
        }

        private bool TurnInPhase()
        {
            if (!FeatureHelper.IsEnabled<AutoSelectTurnin>())
            {
                PrintModuleMessage("Please enable the Auto-Select Turn In feature.");
                EndLoop("Auto-Select Turn In not enabled");
                return false;
            }

            var requiredIngredients = GetRequiredItems();

            if(!HasEnoughItems(requiredIngredients))
            {
                PrintModuleMessage("Not enough items to complete phase");
                EndLoop("Insufficent items");
                return false;
            }

            foreach (var ingredient in requiredIngredients)
            {
                for (var i = ingredient.TurnedInSoFar; i < ingredient.TotalTimesToTurnIn; i++)
                {
                    TaskManager.EnqueueImmediate(() => ClickItem(requiredIngredients.IndexOf(ingredient), ingredient.AmountPerTurnIn), $"SelectingItem{ingredient.Ingredient.Name}");
                    //TaskManager.EnqueueImmediate(() => HandInItem());
                    TaskManager.EnqueueImmediate(() => ConfirmContribution(), "ConfirmingContribution");
                }
            }

            return true;
        }

        private void TurnInProject()
        {
            if (!FeatureHelper.IsEnabled<AutoSelectTurnin>())
            {
                PrintModuleMessage("Please enable the Auto-Select Turn In feature.");
                EndLoop("Auto-Select Turn In not enabled");
                return;
            }

            if (TryGetAddonByName<AtkUnitBase>("SubmarinePartsMenu", out var addon))
            {
                for (var i = addon->AtkValues[6].UInt; i < addon->AtkValues[7].UInt; i++)
                {
                    TaskManager.Enqueue(() => TurnInPhase(), $"TurnInPhase{i}");
                    //TaskManager.Enqueue(i == addon->AtkValues[7].UInt - 1 ? CompleteConstruction : AdvancePhase);
                    TaskManager.Enqueue(WaitForCutscene, "WaitForCutscene");
                    TaskManager.Enqueue(PressEsc, "PressingEsc");
                    TaskManager.Enqueue(ConfirmSkip, "ConfirmCSSkip");
                    if (i != addon->AtkValues[7].UInt - 1)
                    {
                        TaskManager.Enqueue(() => TryGetNearestFabricationPanel(), "TargetingWithFabricationPanel");
                        TaskManager.Enqueue(() => InteractWithFabricationPanel(), "InteractingWithFabricationPanel");
                        TaskManager.Enqueue(ContributeMaterials, "SelectingContributeMaterials");
                    }
                }
            }
            else
            {
                EndLoop("Failed to find SubmarinePartsMenu");
            }
        }

        private List<PartIngredient> GetRequiredItems()
        {
            var ingredients = new List<PartIngredient>();

            if (TryGetAddonByName<AtkUnitBase>("SubmarinePartsMenu", out var addon))
            {
                for (var i = 0; i <= 11; i++)
                {
                    if (addon->AtkValues[36 + i].Type == 0) continue;

                    var itemName = MemoryHelper.ReadSeStringNullTerminated(new nint(addon->AtkValues[36 + i].String)).ToString();

                    ingredients.Add(new PartIngredient(
                        Svc.Data.GetExcelSheet<Item>(Svc.ClientState.ClientLanguage).FirstOrDefault(x => x.Name.RawString.Equals(itemName, StringComparison.CurrentCultureIgnoreCase)),
                        addon->AtkValues[60 + i].UInt,
                        addon->AtkValues[60 + i].UInt * addon->AtkValues[120 + i].UInt,
                        addon->AtkValues[108 + i].UInt,
                        addon->AtkValues[120 + i].UInt
                    ));
                }
            }
            else
            {
                EndLoop("Failed to find SubmarinePartsMenu");
            }
            
            return ingredients;
        }

        private bool HasEnoughItems(List<PartIngredient> requiredIngredients)
        {
            // 36-47 names
            // 60-71 amount per turn in (uint)
            // 108-109 times turned in so far for phase (uint)
            // 120-131 times to turn in for the phase (uint)

            return true;
        }

        private bool ClickItem(int positionInList, uint turnInAmount)
        {
            if (TryGetAddonByName<AtkUnitBase>("Request", out var requestAddon) && requestAddon->IsVisible) return false;

            if (TryGetAddonByName<AtkUnitBase>("SubmarinePartsMenu", out var addon))
            {
                Callback.Fire(addon, false, 0, (uint)positionInList, turnInAmount);
                return TryGetAddonByName<AtkUnitBase>("Request", out var rAddon) && rAddon->IsVisible;
            }
            return false;
        }

        private static bool ConfirmContribution()
        {
            var x = GetSpecificYesno((s) => s.ContainsAny(StringComparison.OrdinalIgnoreCase, "contribute"));
            if (x != null)
            {
                PluginLog.Log("Confirming contribution");
                ClickSelectYesNo.Using((nint)x).Yes();
                return true;
            }
            return false;
        }

        private static bool? ContributeMaterials() =>
            TrySelectSpecificEntry("Contribute materials.", () => GenericThrottle && EzThrottler.Throttle("WorkshopTurnIn.SelectContributeMaterials", 1000));

        private void AutoPhase(SetupAddonArgs obj)
        {
            if (obj.AddonName != "SelectString" || !active) return;

            TaskManager.EnqueueImmediate(() => AdvancePhase() == true || CompleteConstruction() == true, "SelectingNextPhase");
        }

        private static bool? AdvancePhase() =>
            TrySelectSpecificEntry("Advance to the next phase of production.", () => GenericThrottle && EzThrottler.Throttle("WorkshopTurnIn.SelectAdvanceNextPhase", 1000));

        private static bool? CompleteConstruction() =>
            TrySelectSpecificEntry("Complete the construction", () => GenericThrottle && EzThrottler.Throttle("WorkshopTurnIn.SelectCompleteConstruction", 1000));

        private static bool? WaitForCutscene() =>
            Svc.Condition[ConditionFlag.OccupiedInCutSceneEvent] || Svc.Condition[ConditionFlag.WatchingCutscene78];

        private static bool? PressEsc()
        {
            var nLoading = Svc.GameGui.GetAddonByName("NowLoading", 1);
            if (nLoading != nint.Zero)
            {
                var nowLoading = (AtkUnitBase*)nLoading;
                if (nowLoading->IsVisible)
                {
                    //pi.Framework.Gui.Chat.Print(Environment.TickCount + " Now loading visible");
                }
                else
                {
                    //pi.Framework.Gui.Chat.Print(Environment.TickCount + " Now loading not visible");
                    //if (SendKeypress(Keys.Escape))
                    //{
                    //    return true;
                    //}
                }
            }
            return false;
        }

        //private static bool SendKeypress(Keys key)
        //{
        //    if (WindowFunctions.TryFindGameWindow(out var h))
        //    {
        //        InternalLog.Verbose($"Sending key {key}");
        //        User32.SendMessage(h, User32.WindowMessage.WM_KEYDOWN, (int)key, 0);
        //        User32.SendMessage(h, User32.WindowMessage.WM_KEYUP, (int)key, 0);
        //        return true;
        //    }
        //    else
        //    {
        //        PluginLog.Error("Couldn't find game window!");
        //    }
        //    return false;
        //}

        private static bool? ConfirmSkip()
        {
            var addon = Svc.GameGui.GetAddonByName("SelectString", 1);
            if (addon == nint.Zero) return false;
            var selectStrAddon = (AddonSelectString*)addon;
            if (!IsAddonReady(&selectStrAddon->AtkUnitBase))
            {
                return false;
            }
            if (!SkipCutsceneStr.Contains(selectStrAddon->AtkUnitBase.UldManager.NodeList[3]->GetAsAtkTextNode()->NodeText.ToString())) return false;
            if (EzThrottler.Throttle("Voyage.CutsceneSkip"))
            {
                PluginLog.Log("Selecting cutscene skipping");
                ClickSelectString.Using(addon).SelectItem(0);
                return true;
            }
            return false;
        }

        private static bool IsFabricationPanel(GameObject obj) =>
            obj?.Name.ToString().EqualsAny(PanelName) == true;

        private static bool IsFabricationCondition() =>
            Svc.Condition[ConditionFlag.OccupiedInEvent] || Svc.Condition[ConditionFlag.OccupiedInQuestEvent];

        private static bool IsInFabricationPanel() =>
            IsFabricationCondition() && IsFabricationPanel(Svc.Targets.Target);

        private static bool TryGetNearestFabricationPanel() =>
            Svc.Objects.TryGetFirst(x => x.Name.ToString().EqualsAny(PanelName) && x.IsTargetable, out var o);

        private static bool InteractWithFabricationPanel()
        {
            TargetSystem.Instance()->InteractWithObject(TargetSystem.Instance()->Target);
            return TryGetAddonByName<AtkUnitBase>("SubmarinePartsMenu", out var addon) && addon->IsVisible;
        }
    }
}
