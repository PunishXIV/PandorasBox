using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using PandorasBox.UI;
using ClickLib.Clicks;
using Dalamud.Interface;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using System;
using System.Numerics;
using ECommons;
using Dalamud.Game.Text.SeStringHandling;
using Lumina.Excel.GeneratedSheets;
using System.Linq;
using Dalamud.Game.ClientState.Conditions;
using static ECommons.GenericHelpers;
using Dalamud.Memory;

namespace PandorasBox.Features.Achievements
{
    public unsafe class GettingTooAttached : Feature
    {
        public override string Name => "Getting Too Attached";

        public override string Description => "Adds a button to the materia melding window to loop melding for the Getting Too Attached achievements.";

        public override FeatureType FeatureType => FeatureType.Achievements;

        private Overlays overlay;
        private float height;

        internal bool active = false;

        public Configs Config { get; private set; }
        public override bool UseAutoConfig => false;
        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Times to loop", "", 1, IntMin = 0, IntMax = 10000, EditorSize = 300)]
            public int numberOfLoops = 10000;
        }
        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            overlay = new Overlays(this);
            Svc.Toasts.ErrorToast += CheckForErrors;
            Common.OnAddonSetup += ConfirmMateriaDialog;
            Common.OnAddonSetup += ConfirmRetrievalDialog;
            base.Enable();
        }

        public override void Disable()
        {
            SaveConfig(Config);
            P.Ws.RemoveWindow(overlay);
            Svc.Toasts.ErrorToast -= CheckForErrors;
            Common.OnAddonSetup -= ConfirmMateriaDialog;
            Common.OnAddonSetup -= ConfirmRetrievalDialog;
            base.Disable();
        }

        public override void Draw()
        {
            if (TryGetAddonByName<AtkUnitBase>("MateriaAttach", out var addon))
            {
                if (!(addon->UldManager.NodeListCount > 1)) return;
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

                if (ImGui.Button(!active ? $"Getting Too Attached###StartLooping" : $"Looping. Click to abort.###AbortLoop"))
                {
                    if (!active)
                    {
                        active = true;
                        TaskManager.Enqueue(YesAlready.DisableIfNeeded);
                        TaskManager.Enqueue(TryGettingTooAttached);
                    }
                    else
                    {
                        CancelLoop();
                    }
                }

                ImGui.SameLine();
                ImGui.PushItemWidth(150);
                if (ImGui.SliderInt("Loops", ref Config.numberOfLoops, 0, 10000))
                    SaveConfig(Config);

                height = ImGui.GetWindowSize().Y;

                ImGui.End();
                ImGui.PopStyleVar(2);
            }
        }

        private void CancelLoop()
        {
            active = false;
            TaskManager.Abort();
            TaskManager.Enqueue(YesAlready.EnableIfNeeded);
        }

        private void CheckForErrors(ref SeString message, ref bool isHandled)
        {
            var msg = message.ExtractText();
            if (new[] { 7701, 7707 }.Any(x => msg == Svc.Data.GetExcelSheet<LogMessage>().FirstOrDefault(y => y.RowId == x)?.Text.ExtractText()))
            {
                PrintModuleMessage("Error while melding. Aborting Tasks.");
                CancelLoop();
            }
        }

        private static bool IsBusy() => Svc.Condition[ConditionFlag.MeldingMateria] || Svc.Condition[ConditionFlag.Occupied39] || !Svc.Condition[ConditionFlag.NormalConditions];

        private void TryGettingTooAttached()
        {
            if (Config.numberOfLoops > 0)
            {
                TaskManager.Enqueue(SelectItem, "Selecting Item");
                TaskManager.Enqueue(SelectMateria, "Selecting Materia");
                TaskManager.Enqueue(SelectItem, "Selecting Item");
                TaskManager.Enqueue(() => ActivateContextMenu(), "Opening Context Menu");
                TaskManager.Enqueue(() => RetrieveMateriaContextMenu(), "Activating Retrieve Materia Context Entry");
                TaskManager.Enqueue(() => Config.numberOfLoops -= 1);
                TaskManager.Enqueue(TryGettingTooAttached, "Repeat Loop");
            }
            else
            {
                TaskManager.Enqueue(() => active = false);
                TaskManager.Enqueue(YesAlready.EnableIfNeeded);
            }
        }

        private unsafe bool? SelectItem()
        {
            if (TryGetAddonByName<AtkUnitBase>("MateriaAttach", out var addon) && !IsBusy() && !AreDialogsOpen())
            {
                if (addon->UldManager.NodeList[16]->IsVisible)
                {
                    CancelLoop();
                    PrintModuleMessage("Unable to continue. No gear in inventory");
                    return false;
                }

                Callback.Fire(addon, false, 1, 0, 1, 0); // second value is what position in the list the item is
                return addon->AtkValues[287].Int != -1; // condition for something being selected
            }
            return false;
        }

        public static bool AreDialogsOpen() => Svc.GameGui.GetAddonByName("MateriaAttachDialog") != IntPtr.Zero && Svc.GameGui.GetAddonByName("MateriaRetrieveDialog") != IntPtr.Zero;

        public unsafe bool? SelectMateria()
        {
            if (TryGetAddonByName<AtkUnitBase>("MateriaAttach", out var addon) && !AreDialogsOpen())
            {
                if (addon->UldManager.NodeList[6]->IsVisible)
                {
                    CancelLoop();
                    PrintModuleMessage("Unable to continue. No materia to meld.");
                    return false;
                }
                else if (MemoryHelper.ReadSeStringNullTerminated(new nint(addon->AtkValues[289].String)).ToString().Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)[2] == "0")
                {
                    CancelLoop();
                    PrintModuleMessage("Unable to continue. First listed materia has too high ilvl requirements.");
                    return false;
                }

                Callback.Fire(addon, false, 2, 0, 1, 0); // second value is which materia in the list
                return TryGetAddonByName<AtkUnitBase>("MateriaAttachDialog", out var attachDialog) && attachDialog->IsVisible && Svc.Condition[ConditionFlag.MeldingMateria];
            }
            return false;
        }

        public void ConfirmMateriaDialog(SetupAddonArgs obj)
        {
            if (obj.AddonName != "MateriaAttachDialog" || !active) return;
            if (obj.Addon->AtkValues[50].Type != 0)
            {
                CancelLoop();
                PrintModuleMessage("Unable to continue. This gear is requires overmelding.");
                return;
            }

            TaskManager.EnqueueImmediate(() => Svc.Condition[ConditionFlag.MeldingMateria]);
            TaskManager.EnqueueImmediate(() => Callback.Fire(obj.Addon, true, 0, 0, 0));
        }

        public unsafe bool ActivateContextMenu()
        {
            if (TryGetAddonByName<AtkUnitBase>("MateriaAttach", out var addon) && !Svc.Condition[ConditionFlag.MeldingMateria])
            {
                Callback.Fire(addon, false, 4, 0, 1, 0);

                return TryGetAddonByName<AtkUnitBase>("ContextMenu", out var contextMenu) && contextMenu->IsVisible;
            }
            return false;
        }

        private static bool RetrieveMateriaContextMenu()
        {
            if (!Svc.Condition[ConditionFlag.Occupied39])
                Callback.Fire((AtkUnitBase*)Svc.GameGui.GetAddonByName("ContextMenu"), true, 0, 1, 0u, 0, 0);
            return !Svc.Condition[ConditionFlag.Occupied39];
        }

        public void ConfirmRetrievalDialog(SetupAddonArgs obj)
        {
            if (obj.AddonName != "MateriaRetrieveDialog" || !active) return;
            ClickMateriaRetrieveDialog.Using((IntPtr)obj.Addon).Begin();
        }
    }
}
