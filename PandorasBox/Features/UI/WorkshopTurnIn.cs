using AutoRetainer.Modules;
using ClickLib.Clicks;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Interface;
using Dalamud.Memory;
using Dalamud.Utility;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Logging;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
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

        internal static bool active = false;
        internal bool phaseActive = false;
        internal bool projectActive = false;
        internal bool partLoopActive = false;
        private static readonly string[] SkipCutsceneStr = { "Skip cutscene?", "要跳过这段过场动画吗？", "要跳過這段過場動畫嗎？", "Videosequenz überspringen?", "Passer la scène cinématique ?", "このカットシーンをスキップしますか？" };
        internal static string[] PanelName = new string[] { "Fabrication Station" };

        public Configs Config { get; private set; }
        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Times to loop", "", 1, IntMin = 0, IntMax = 100, EditorSize = 300)]
            public int partsToBuild = 1;
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            overlay = new Overlays(this);
            Svc.Framework.Update += Tick;
            base.Enable();
        }

        public override void Disable()
        {
            SaveConfig(Config);
            P.Ws.RemoveWindow(overlay);
            Svc.Framework.Update -= Tick;
            base.Disable();
        }

        public readonly struct PartIngredient(Item ingredient, uint reqLevel, ClassJob reqJob, uint inventory, uint perturn, uint total, uint timesSoFar, uint timesTotal)
        {
            public Item Ingredient { get; } = ingredient;
            public uint RequiredLevelToTurnIn { get; } = reqLevel;
            public ClassJob RequiredJobToTurnIn { get; } = reqJob;
            public uint AmountInInventory { get; } = inventory;
            public uint AmountPerTurnIn { get; } = perturn;
            public uint TotalRequiredAmount { get; } = total;
            public uint TurnedInSoFar { get; } = timesSoFar;
            public uint TotalTimesToTurnIn { get; } = timesTotal;
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool hasChanged) =>
        {
            if (ImGui.Button("InteractWithPanel")) InteractWithFabricationPanel();
        };

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
                        TurnInPhase();
                    }
                    else
                    {
                        EndLoop("User cancelled");
                    }
                }

                if (active && !phaseActive) ImGui.EndDisabled();

                ImGui.SameLine();

                if (active && !projectActive) ImGui.BeginDisabled();

                if (ImGui.Button(!projectActive ? $"Project Turn In###StartProjectLooping" : $"Turning in... Click to Abort###AbortProjectLoop"))
                {
                    if (!projectActive)
                    {
                        projectActive = true;
                        TurnInProject();
                    }
                    else
                    {
                        EndLoop("User cancelled");
                    }
                }

                if (active && !projectActive) ImGui.EndDisabled();

                //ImGui.SameLine();

                //if (active && !partLoopActive) ImGui.BeginDisabled();

                //if (ImGui.Button(!partLoopActive ? $"Part Loop Turn In###StartPartLooping" : $"Turning in... Click to Abort###AbortPartLoop"))
                //{
                //    if (!partLoopActive)
                //    {
                //        partLoopActive = true;
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

        private void Tick(Framework framework)
        {
            TextAdvanceManager.Tick();
            YesAlready.Tick();
        }

        private bool MustEndLoop(bool condition, string message)
        {
            if (condition)
            {
                PrintModuleMessage(message);
                EndLoop(message);
            }
            return condition;
        }

        private bool EndLoop(string msg)
        {
            PluginLog.Log($"Cancelling... Reason: {msg}");
            active = false;
            phaseActive = false;
            projectActive = false;
            partLoopActive = false;
            TaskManager.Abort();
            return true;
        }

        private bool TurnInPhase()
        {
            if (!FeatureHelper.IsEnabled<AutoSelectTurnin>())
            {
                PrintModuleMessage("Please enable the Auto-Select Turn In feature.");
                EndLoop("Auto-Select Turn In not enabled");
                return true;
            }

            var requiredIngredients = GetRequiredItems();
            if (requiredIngredients?.Count == 0) return true;

            if (MustEndLoop(!IsSufficientlyLeveled(requiredIngredients), "Not high enough level to turn in items") ||
                MustEndLoop(Svc.ClientState.LocalPlayer.ClassJob.Id is < 8 or > 15, "Must be a DoH to turn in items."))
            {
                return true;
            }

            foreach (var ingredient in requiredIngredients)
            {
                if (ingredient.AmountPerTurnIn > ingredient.AmountInInventory) continue;

                for (var i = ingredient.TurnedInSoFar; i < ingredient.TotalTimesToTurnIn; i++)
                {
                    TaskManager.EnqueueImmediate(() => ClickItem(requiredIngredients.IndexOf(ingredient), ingredient.AmountPerTurnIn), $"SelectingItem{ingredient.Ingredient.Name}");
                    TaskManager.EnqueueImmediate(() => ConfirmContribution(), "ConfirmingContribution");
                    TaskManager.EnqueueImmediate(() => ConfirmHQTrade(), "ConfirmingHQTrade");
                }
            }

            if (phaseActive) TaskManager.Enqueue(() => EndLoop("Finished Task"));
            return true;
        }

        private bool TurnInProject()
        {
            if (!FeatureHelper.IsEnabled<AutoSelectTurnin>())
            {
                PrintModuleMessage("Please enable the Auto-Select Turn In feature.");
                EndLoop("Auto-Select Turn In not enabled");
                return true;
            }

            if (TryGetAddonByName<AtkUnitBase>("SubmarinePartsMenu", out var addon))
            {
                for (var i = addon->AtkValues[6].UInt; i < addon->AtkValues[7].UInt; i++)
                {
                    TaskManager.Enqueue(() => TurnInPhase(), $"TurnInPhase{i}");
                    TaskManager.Enqueue(i == addon->AtkValues[7].UInt - 1 ? CompleteConstruction : AdvancePhase);
                    TaskManager.Enqueue(WaitForCutscene, "WaitForCutscene");
                    TaskManager.Enqueue(PressEsc, "PressingEsc");
                    TaskManager.Enqueue(ConfirmSkip, "ConfirmCSSkip");
                    if (i != addon->AtkValues[7].UInt - 1)
                    {
                        TaskManager.Enqueue(InteractWithFabricationPanel, "InteractingWithFabricationPanel");
                        TaskManager.Enqueue(ContributeMaterials, "SelectingContributeMaterials");
                    }
                }
            }
            else
            {
                EndLoop("Failed to find SubmarinePartsMenu");
                return true;
            }

            if (projectActive) TaskManager.Enqueue(() => EndLoop("Finished Task"));
            return true;
        }

        private List<PartIngredient> GetRequiredItems()
        {
            // 12-23 item ids
            // 36-47 names
            // 60-71 amount per turn in (uint)
            // 72-83 amount in inventory (uint)
            // 84-95 amount in inventory, nq/hq split (string)
            // 108-119 times turned in so far for phase (uint)
            // 120-131 times to turn in for the phase (uint)
            // 144-156 level requirement to turn in part (uint)
            if (!TryGetAddonByName<AtkUnitBase>("SubmarinePartsMenu", out var addon))
            {
                EndLoop("Failed to find SubmarinePartsMenu");
                return null;
            }

            return Enumerable.Range(0, 12)
                .Where(i => addon->AtkValues[36 + i].Type != 0)
                .Select(i => new PartIngredient(
                    Svc.Data.GetExcelSheet<Item>(Svc.ClientState.ClientLanguage).GetRow(addon->AtkValues[12 + i].UInt),
                    addon->AtkValues[144 + i].UInt,
                    Svc.Data.GetExcelSheet<ClassJob>(Svc.ClientState.ClientLanguage).FirstOrDefault(x => x.RowId == addon->AtkValues[48 + i].Int - 62000),
                    addon->AtkValues[72 + i].UInt,
                    addon->AtkValues[60 + i].UInt,
                    addon->AtkValues[60 + i].UInt * addon->AtkValues[120 + i].UInt,
                    addon->AtkValues[108 + i].UInt,
                    addon->AtkValues[120 + i].UInt
                )).ToList();
        }


        private static bool IsSufficientlyLeveled(List<PartIngredient> requiredIngredients)
        {
            foreach (var i in requiredIngredients)
                if (PlayerState.Instance()->ClassJobLevelArray[i.RequiredJobToTurnIn.ExpArrayIndex] < i.RequiredLevelToTurnIn)
                    return false;

            return true;
        }

        private bool ClickItem(int positionInList, uint turnInAmount)
        {
            // I cannot detect if you have the right amount of items but not within a sufficiently large single stack
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

        private static bool ConfirmHQTrade()
        {
            var x = GetSpecificYesno((s) => s.ContainsAny(StringComparison.OrdinalIgnoreCase, Svc.Data.GetExcelSheet<Addon>(Svc.ClientState.ClientLanguage).GetRow(102434).Text.ToDalamudString().ExtractText()));
            if (x != null)
            {
                PluginLog.Log("Confirming HQ Trade");
                ClickSelectYesNo.Using((nint)x).Yes();
                return true;
            }
            return true;
        }

        private static bool? ContributeMaterials() =>
            TrySelectSpecificEntry("Contribute materials.", () => GenericThrottle && EzThrottler.Throttle("WorkshopTurnIn.SelectContributeMaterials", 1000));

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
                    if (WindowsKeypress.SendKeypress(Keys.Escape))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

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

        internal static bool TryGetNearestFabricationPanel(out Dalamud.Game.ClientState.Objects.Types.GameObject obj) =>
            Svc.Objects.TryGetFirst(x => x.Name.ToString().EqualsAny(PanelName) && x.IsTargetable, out obj);

        internal static bool? InteractWithFabricationPanel()
        {
            if (!Svc.Condition[ConditionFlag.NormalConditions]) return false;

            if (TryGetNearestFabricationPanel(out var obj) && Svc.Targets.Target?.Address == obj.Address)
            {
                if (GenericThrottle && EzThrottler.Throttle("WorkshopTurnIn.InteractWithFabricationPanel", 2000))
                {
                    TargetSystem.Instance()->InteractWithObject(obj.Struct(), false);
                    return true;
                }
                else if (obj.IsTargetable && GenericThrottle)
                    Svc.Targets.Target = obj;
            }
            return false;
        }
    }
}
