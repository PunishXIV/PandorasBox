using System;
using System.Linq;
using System.Text;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Logging;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;

namespace PandorasBox.Features.UI
{
    public unsafe class AutoNumerics : Feature
    {
        public override string Name => "Auto-Fill Numeric Dialogs";

        public override string Description => "Automatically fills any numeric input dialog boxes. Works on a whitelist system. Hold shift when opening a numeric dialog to disable.";

        public override FeatureType FeatureType => FeatureType.UI;

        public Configs Config { get; private set; }

        private readonly string splitText = Svc.Data.GetExcelSheet<Addon>().Where(x => x.RowId == 533).First().Text.RawString;

        private bool hasDisabled;

        public class Configs : FeatureConfig
        {
            public bool WorkOnTrading = false;
            public int TradeMinOrMax = -1;
            public bool TradeExcludeSplit = false;
            public bool TradeConfirm = false;

            public bool WorkOnFCChest = false;
            public int FCChestMinOrMax = 1;
            public bool FCExcludeSplit = true;
            public bool FCChestConfirm = false;

            public bool WorkOnRetainers = false;
            public int RetainersMinOrMax = -1;
            public bool RetainerExcludeSplit = false;
            public bool RetainersConfirm = false;

            public bool WorkOnInventory = false;
            public int InventoryMinOrMax = -1;
            public bool InventoryExcludeSplit = false;
            public bool InventoryConfirm = false;

            public bool WorkOnMail = false;
            public int MailMinOrMax = -1;
            public bool MailExcludeSplit = false;
            public bool MailConfirm = false;

            public bool WorkOnTransmute = false;
            public int TransmuteMinOrMax = 0;
            public bool TransmuteExcludeSplit = true;
            public bool TransmuteConfirm = true;

            public bool WorkOnVentures = false;
            public int VentureMinOrMax = 1;
            public bool VentureExcludeSplit = true;
            public bool VentureConfirm = false;
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Common.OnAddonSetup += FillRegularNumeric;
            Common.OnAddonSetup += FillVentureNumeric;
            //Common.OnAddonSetup += FillBankNumeric;
            base.Enable();
        }

        private void FillRegularNumeric(SetupAddonArgs obj)
        {
            if (obj.AddonName != "InputNumeric") return;
            if (ImGui.GetIO().KeyShift) return;

            try
            {
                var minValue = obj.Addon->AtkValues[2].Int;
                var maxValue = obj.Addon->AtkValues[3].Int;
                var numericTextNode = obj.Addon->UldManager.NodeList[4]->GetAsAtkComponentNode()->Component->UldManager.NodeList[4]->GetAsAtkTextNode();
                var numericResNode = obj.Addon->UldManager.NodeList[4]->GetAsAtkComponentNode()->Component->UldManager.NodeList[6];

                if (Config.WorkOnTrading && Svc.Condition[ConditionFlag.TradeOpen])
                    TryFill(obj.Addon, minValue, maxValue, Config.TradeMinOrMax, Config.TradeExcludeSplit, Config.TradeConfirm);

                if (Config.WorkOnFCChest && InFcChest())
                    TryFill(obj.Addon, minValue, maxValue, Config.FCChestMinOrMax, Config.FCExcludeSplit, Config.FCChestConfirm);

                if (Config.WorkOnRetainers && Svc.Condition[ConditionFlag.OccupiedSummoningBell] && !InFcChest())
                    TryFill(obj.Addon, minValue, maxValue, Config.RetainersMinOrMax, Config.RetainerExcludeSplit, Config.RetainersConfirm);

                if (Config.WorkOnMail && InMail())
                    TryFill(obj.Addon, minValue, maxValue, Config.MailMinOrMax, Config.MailExcludeSplit, Config.MailConfirm);

                if (Config.WorkOnTransmute && InTransmute())
                    TryFill(obj.Addon, minValue, maxValue, Config.TransmuteMinOrMax, Config.TransmuteExcludeSplit, Config.TransmuteConfirm);
            }
            catch (Exception ex)
            {
                ex.Log();
            }
        }

        private void TryFill(AtkUnitBase* numeric, int minValue, int maxValue, int minOrMax, bool excludeSplit, bool autoConfirm)
        {
            var numericTextNode = numeric->UldManager.NodeList[4]->GetAsAtkComponentNode()->Component->UldManager.NodeList[4]->GetAsAtkTextNode();
            var numericResNode = numeric->UldManager.NodeList[4]->GetAsAtkComponentNode()->Component->UldManager.NodeList[6];

            if (excludeSplit && IsSplitAddon()) return;
            if (minOrMax == 0)
            {
                TaskManager.Enqueue(() => numericTextNode->SetText(ConvertToByte(minValue)));
                if (autoConfirm)
                    TaskManager.Enqueue(() => Callback.Fire(numeric, true, minValue));
            }
            if (minOrMax == 1)
            {
                TaskManager.Enqueue(() => numericTextNode->SetText(ConvertToByte(maxValue)));
                if (autoConfirm)
                    TaskManager.Enqueue(() => Callback.Fire(numeric, true, maxValue));
            }
            if (minOrMax == -1)
            {
                var currentAmt = numericTextNode->NodeText.ToString();
                if (int.TryParse(currentAmt, out var num) && num > 0 && !numericResNode->IsVisible)
                    TaskManager.Enqueue(() => Callback.Fire(numeric, true, int.Parse(currentAmt)));
            }
        }

        private void FillBankNumeric(SetupAddonArgs obj)
        {
            if (obj.AddonName != "Bank") return;
            if (ImGui.GetIO().KeyShift) return;
            if (!Config.WorkOnFCChest) return;

            try
            {
                var bMinValue = obj.Addon->AtkValues[5].Int;
                var bMaxValue = obj.Addon->AtkValues[6].Int;
                var bNumericTextNode = obj.Addon->UldManager.NodeList[4]->GetAsAtkComponentNode()->Component->UldManager.NodeList[4]->GetAsAtkTextNode();

                if (Config.FCExcludeSplit && IsSplitAddon()) { return; }
                if (Config.FCChestMinOrMax == 0)
                {
                    TaskManager.Enqueue(() => bNumericTextNode->SetText(ConvertToByte(bMinValue)));
                    if (Config.FCChestConfirm)
                        TaskManager.Enqueue(() => Callback.Fire(obj.Addon, true, 3, (uint)bMinValue));
                }
                if (Config.FCChestMinOrMax == 1)
                {
                    TaskManager.Enqueue(() => bNumericTextNode->SetText(ConvertToByte(bMaxValue)));
                    if (Config.FCChestConfirm)
                        TaskManager.Enqueue(() => Callback.Fire(obj.Addon, true, 3, (uint)bMaxValue));
                }
                if (Config.FCChestMinOrMax == -1)
                {
                    var currentAmt = bNumericTextNode->NodeText.ToString();
                    if (int.TryParse(currentAmt, out var num) && num > 0 && obj.Addon->AtkValues[4].Int > 0)
                        TaskManager.Enqueue(() => Callback.Fire(obj.Addon, true, 0));
                }
            }
            catch
            {
                return;
            }
        }

        private void FillVentureNumeric(SetupAddonArgs obj)
        {
            if (obj.AddonName != "ShopExchangeCurrencyDialog") return;
            if (ImGui.GetIO().KeyShift) return;
            if (!Config.WorkOnVentures) return;

            try
            {
                var minValue = 1;
                var maxAvailable = obj.Addon->AtkValues[5].UInt - obj.Addon->AtkValues[4].UInt;
                var maxAfford = obj.Addon->AtkValues[1].UInt / obj.Addon->AtkValues[2].UInt;
                var maxValue = maxAvailable > maxAfford ? maxAfford : maxAvailable;

                var numericTextNode = obj.Addon->UldManager.NodeList[8]->GetAsAtkComponentNode()->Component->UldManager.NodeList[4]->GetAsAtkTextNode();

                if (Config.VentureMinOrMax == 0)
                {
                    TaskManager.Enqueue(() => numericTextNode->SetText(ConvertToByte(minValue)));
                    if (Config.VentureConfirm)
                        TaskManager.Enqueue(() => Callback.Fire(obj.Addon, true, 0, minValue));
                }
                if (Config.VentureMinOrMax == 1)
                {
                    TaskManager.Enqueue(() => numericTextNode->SetText(ConvertToByte((int)maxValue)));
                    if (Config.VentureConfirm)
                        TaskManager.Enqueue(() => Callback.Fire(obj.Addon, true, 0, maxValue));
                }

                // No way to detect manually entered amounts
            }
            catch (Exception ex)
            {
                ex.Log();
            }
        }

        private bool IsSplitAddon()
        {
            var numeric = (AtkUnitBase*)Svc.GameGui.GetAddonByName("InputNumeric");
            var numericTitleText = numeric->UldManager.NodeList[5]->GetAsAtkTextNode()->NodeText.ToString();
            return numericTitleText == splitText;
        }

        private bool InFcChest()
        {
            var fcChest = (AtkUnitBase*)Svc.GameGui.GetAddonByName("FreeCompanyChest");
            return fcChest != null && fcChest->IsVisible;
        }

        private bool InFcBank()
        {
            var fcBank = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Bank");
            return fcBank != null && fcBank->IsVisible;
        }

        private bool InMail()
        {
            var mail = (AtkUnitBase*)Svc.GameGui.GetAddonByName("LetterList");
            return mail != null && mail->IsVisible;
        }

        private bool InTransmute()
        {
            var trans = (AtkUnitBase*)Svc.GameGui.GetAddonByName("TradeMultiple");
            return trans != null && trans->IsVisible;
        }

        private unsafe byte* ConvertToByte(int x)
        {
            var bArray = Encoding.Default.GetBytes(x.ToString());
            byte* ptr;
            fixed (byte* tmpPtr = bArray) { ptr = tmpPtr; }
            return ptr;
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Common.OnAddonSetup -= FillRegularNumeric;
            Common.OnAddonSetup -= FillVentureNumeric;
            //Common.OnAddonSetup -= FillBankNumeric;
            base.Disable();
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool _) =>
        {
            DrawConfigsForAddon("Trading", ref Config.WorkOnTrading, ref Config.TradeMinOrMax, ref Config.TradeExcludeSplit, ref Config.TradeConfirm);
            DrawConfigsForAddon("FC Chests", ref Config.WorkOnFCChest, ref Config.FCChestMinOrMax, ref Config.FCExcludeSplit, ref Config.FCChestConfirm);
            DrawConfigsForAddon("Retainers", ref Config.WorkOnRetainers, ref Config.RetainersMinOrMax, ref Config.RetainerExcludeSplit, ref Config.RetainersConfirm);
            DrawConfigsForAddon("Mail", ref Config.WorkOnMail, ref Config.MailMinOrMax, ref Config.MailExcludeSplit, ref Config.MailConfirm);
            DrawConfigsForAddon("Materia Transmutation", ref Config.WorkOnTransmute, ref Config.TransmuteMinOrMax, ref Config.TransmuteExcludeSplit, ref Config.TransmuteConfirm);
            DrawConfigsForAddon("Venture Purchase", ref Config.WorkOnVentures, ref Config.VentureMinOrMax, ref Config.VentureExcludeSplit, ref Config.VentureConfirm);
        };

        private static void DrawConfigsForAddon(string addonName, ref bool workOnAddon, ref int minOrMax, ref bool excludeSplit, ref bool autoConfirm)
        {
            ImGui.Checkbox($"Work on {addonName}", ref workOnAddon);
            if (workOnAddon)
            {
                ImGui.PushID(addonName);
                ImGui.Indent();
                if (ImGui.RadioButton($"Auto fill highest amount possible", minOrMax == 1))
                {
                    minOrMax = 1;
                }
                if (ImGui.RadioButton($"Auto fill lowest amount possible", minOrMax == 0))
                {
                    minOrMax = 0;
                }
                if (addonName != "Venture Purchase")
                {
                    if (ImGui.RadioButton($"Auto OK on manually entered amounts", minOrMax == -1))
                    {
                        minOrMax = -1;
                    }

                    ImGui.Checkbox("Exclude Split Dialog", ref excludeSplit);
                }
                if (minOrMax != -1) ImGui.Checkbox("Auto Confirm", ref autoConfirm);
                ImGui.Unindent();
                ImGui.PopID();
            }
        }
    }
}
