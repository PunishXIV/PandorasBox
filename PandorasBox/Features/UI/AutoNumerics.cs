using System;
using System.Linq;
using System.Text;
using Dalamud.Game.ClientState.Conditions;
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

        public override string Description => "Automatically fills any numeric input dialog boxes. Works on a whitelist system. Hold shift when opening numeric to disable.";

        public override FeatureType FeatureType => FeatureType.UI;

        public Configs Config { get; private set; }

        private string splitText = Svc.Data.GetExcelSheet<Addon>().Where(x => x.RowId == 533).First().Text.RawString;

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
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }


        private void RunFeature(Dalamud.Game.Framework framework)
        {
            var numeric = (AtkUnitBase*)Svc.GameGui.GetAddonByName("InputNumeric");
            if (numeric == null) { hasDisabled = false; return; }
            var numericTitleNode = numeric->UldManager.NodeList[5]->GetAsAtkTextNode();

            if (numeric->IsVisible && ECommons.GenericHelpers.IsAddonReady(numeric) && ImGui.GetIO().KeyShift) { hasDisabled = true; return; }

            if (numeric->IsVisible && ECommons.GenericHelpers.IsAddonReady(numeric) && !hasDisabled)
            {
                try
                {
                    var minValue = numeric->AtkValues[2].Int;
                    var maxValue = numeric->AtkValues[3].Int;
                    var numericTextNode = numeric->UldManager.NodeList[4]->GetAsAtkComponentNode()->Component->UldManager.NodeList[4]->GetAsAtkTextNode();
                    var numericResNode = numeric->UldManager.NodeList[4]->GetAsAtkComponentNode()->Component->UldManager.NodeList[6];

                    if (Svc.Condition[ConditionFlag.TradeOpen])
                    {
                        if (Config.TradeExcludeSplit && IsSplitAddon()) return;
                        if (Config.TradeMinOrMax == 0)
                        {
                            TaskManager.Enqueue(() => numericTextNode->SetText(ConvertToByte(minValue)));
                            if (Config.TradeConfirm)
                                TaskManager.Enqueue(() => Callback.Fire(numeric, true, minValue));
                        }
                        if (Config.TradeMinOrMax == 1)
                        {
                            TaskManager.Enqueue(() => numericTextNode->SetText(ConvertToByte(maxValue)));
                            if (Config.TradeConfirm)
                                TaskManager.Enqueue(() => Callback.Fire(numeric, true, maxValue));
                        }
                        if (Config.TradeMinOrMax == -1)
                        {
                            var currentAmt = numericTextNode->NodeText.ToString();
                            if (int.TryParse(currentAmt, out var num) && num > 0 && !numericResNode->IsVisible)
                                TaskManager.Enqueue(() => Callback.Fire(numeric, true, int.Parse(currentAmt)));
                        }
                    }

                    if (InFcChest())
                    {
                        if (Config.FCExcludeSplit && IsSplitAddon()) { return; }
                        if (Config.FCChestMinOrMax == 0)
                        {
                            TaskManager.Enqueue(() => numericTextNode->SetText(ConvertToByte(minValue)));
                            if (Config.FCChestConfirm)
                                TaskManager.Enqueue(() => Callback.Fire(numeric, true, minValue));
                        }
                        if (Config.FCChestMinOrMax == 1)
                        {
                            TaskManager.Enqueue(() => numericTextNode->SetText(ConvertToByte(maxValue)));
                            if (Config.FCChestConfirm)
                                TaskManager.Enqueue(() => Callback.Fire(numeric, true, maxValue));
                        }
                        if (Config.FCChestMinOrMax == -1)
                        {
                            var currentAmt = numericTextNode->NodeText.ToString();
                            if (int.TryParse(currentAmt, out var num) && num > 0 && !numericResNode->IsVisible)
                                TaskManager.Enqueue(() => Callback.Fire(numeric, true, int.Parse(currentAmt)));
                        }
                    }

                    if (Svc.Condition[ConditionFlag.OccupiedSummoningBell] && !InFcChest())
                    {
                        if (Config.RetainerExcludeSplit && IsSplitAddon()) return;
                        if (Config.RetainersMinOrMax == 0)
                        {
                            TaskManager.Enqueue(() => numericTextNode->SetText(ConvertToByte(minValue)));
                            if (Config.RetainersConfirm)
                                TaskManager.Enqueue(() => Callback.Fire(numeric, true, minValue));
                        }
                        if (Config.RetainersMinOrMax == 1)
                        {
                            TaskManager.Enqueue(() => numericTextNode->SetText(ConvertToByte(maxValue)));
                            if (Config.RetainersConfirm)
                                TaskManager.Enqueue(() => Callback.Fire(numeric, true, maxValue));
                        }
                        if (Config.RetainersMinOrMax == -1)
                        {
                            var currentAmt = numericTextNode->NodeText.ToString();
                            if (int.TryParse(currentAmt, out var num) && num > 0 && !numericResNode->IsVisible)
                                TaskManager.Enqueue(() => Callback.Fire(numeric, true, int.Parse(currentAmt)));
                        }
                    }

                    // if ([insert inventory conditions])
                    // {
                    // if (Config.InventoryExcludeSplit && IsSplitAddon()) return;
                    // if (Config.InventoryMinOrMax == 0)
                    //     {
                    //         TaskManager.Enqueue(() => numericTextNode->SetText(ConvertToByte(minValue)));
                    //         if (Config.InventoryConfirm)
                    //             TaskManager.Enqueue(() => Callback.Fire(numeric, true, minValue));
                    //     }
                    //     if (Config.InventoryMinOrMax == 1)
                    //     {
                    //         TaskManager.Enqueue(() => numericTextNode->SetText(ConvertToByte(maxValue)));
                    //         if (Config.InventoryConfirm)
                    //             TaskManager.Enqueue(() => Callback.Fire(numeric, true, maxValue));
                    //     }
                    //     if (Config.InventoryMinOrMax == -1)
                    //     {
                    //         var currentAmt = numericTextNode->NodeText.ToString();
                    //         if (int.TryParse(currentAmt, out var num) && num > 0 && !numericResNode->IsVisible)
                    //             TaskManager.Enqueue(() => Callback.Fire(numeric, true, int.Parse(currentAmt)));
                    //     }
                    // }


                    if (InMail())
                    {
                        if (Config.MailExcludeSplit && IsSplitAddon()) return;
                        if (Config.MailMinOrMax == 0)
                        {
                            TaskManager.Enqueue(() => numericTextNode->SetText(ConvertToByte(minValue)));
                            if (Config.MailConfirm)
                                TaskManager.Enqueue(() => Callback.Fire(numeric, true, minValue));
                        }
                        if (Config.MailMinOrMax == 1)
                        {
                            TaskManager.Enqueue(() => numericTextNode->SetText(ConvertToByte(maxValue)));
                            if (Config.MailConfirm)
                                TaskManager.Enqueue(() => Callback.Fire(numeric, true, maxValue));
                        }
                        if (Config.MailMinOrMax == -1)
                        {
                            var currentAmt = numericTextNode->NodeText.ToString();
                            if (int.TryParse(currentAmt, out var num) && num > 0 && !numericResNode->IsVisible)
                                TaskManager.Enqueue(() => Callback.Fire(numeric, true, int.Parse(currentAmt)));
                        }
                    }
                }
                catch
                {
                    return;
                }
            }
            else
            {
                TaskManager.Abort();
                return;
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

        private bool InMail()
        {
            var mail = (AtkUnitBase*)Svc.GameGui.GetAddonByName("LetterList");
            return mail != null && mail->IsVisible;
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
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool _) =>
        {
            ImGui.Checkbox("Work on Trading", ref Config.WorkOnTrading);
            if (Config.WorkOnTrading)
            {
                ImGui.PushID("Trades");
                ImGui.Indent();
                if (ImGui.RadioButton($"Auto fill highest amount possible", Config.TradeMinOrMax == 1))
                {
                    Config.TradeMinOrMax = 1;
                }
                if (ImGui.RadioButton($"Auto fill lowest amount possible", Config.TradeMinOrMax == 0))
                {
                    Config.TradeMinOrMax = 0;
                }
                if (ImGui.RadioButton($"Auto OK on manually entered amounts", Config.TradeMinOrMax == -1))
                {
                    Config.TradeMinOrMax = -1;
                }
                ImGui.Checkbox("Exclude Split Dialog", ref Config.TradeExcludeSplit);
                if (Config.TradeMinOrMax != -1) ImGui.Checkbox("Auto Confirm", ref Config.TradeConfirm);
                ImGui.Unindent();
                ImGui.PopID();
            }

            ImGui.Checkbox("Work on Company Chests", ref Config.WorkOnFCChest);
            if (Config.WorkOnFCChest)
            {
                ImGui.PushID("FCChests");
                ImGui.Indent();
                if (ImGui.RadioButton($"Auto fill highest amount possible", Config.FCChestMinOrMax == 1))
                {
                    Config.FCChestMinOrMax = 1;
                }
                if (ImGui.RadioButton($"Auto fill lowest amount possible", Config.FCChestMinOrMax == 0))
                {
                    Config.FCChestMinOrMax = 0;
                }
                if (ImGui.RadioButton($"Auto OK on manually entered amounts", Config.FCChestMinOrMax == -1))
                {
                    Config.FCChestMinOrMax = -1;
                }
                ImGui.Checkbox("Exclude Split Dialog", ref Config.FCExcludeSplit);
                if (Config.FCChestMinOrMax != -1) ImGui.Checkbox("Auto Confirm", ref Config.FCChestConfirm);
                ImGui.Unindent();
                ImGui.PopID();
            }

            ImGui.Checkbox("Work on Retainers", ref Config.WorkOnRetainers);
            if (Config.WorkOnRetainers)
            {
                ImGui.PushID("Retainers");
                ImGui.Indent();
                if (ImGui.RadioButton($"Auto fill highest amount possible", Config.RetainersMinOrMax == 1))
                {
                    Config.RetainersMinOrMax = 1;
                }
                if (ImGui.RadioButton($"Auto fill lowest amount possible", Config.RetainersMinOrMax == 0))
                {
                    Config.RetainersMinOrMax = 0;
                }
                if (ImGui.RadioButton($"Auto OK on manually entered amounts", Config.RetainersMinOrMax == -1))
                {
                    Config.RetainersMinOrMax = -1;
                }
                ImGui.Checkbox("Exclude Split Dialog", ref Config.RetainerExcludeSplit);
                if (Config.RetainersMinOrMax != -1) ImGui.Checkbox("Auto Confirm", ref Config.RetainersConfirm);
                ImGui.Unindent();
                ImGui.PopID();
            }

            // ImGui.Checkbox("Work on Player Inventory", ref Config.WorkOnInventory);
            // if (Config.WorkOnInventory)
            // {
            // ImGui.PushID("Inventory");
            // ImGui.Indent();
            //     if (ImGui.RadioButton($"Auto fill highest amount possible", Config.InventoryMinOrMax == 1))
            //     {
            //         Config.InventoryMinOrMax = 1;
            //     }
            //     if (ImGui.RadioButton($"Auto fill lowest amount possible", Config.InventoryMinOrMax == 0))
            //     {
            //         Config.InventoryMinOrMax = 0;
            //     }
            //     if (ImGui.RadioButton($"Auto OK on manually entered amounts", Config.InventoryMinOrMax == -1))
            //     {
            //         Config.InventoryMinOrMax = -1;
            //     }
            //     ImGui.Checkbox("Exclude Split Dialog", ref Config.InventoryExcludeSplit);
            //     if (Config.InventoryMinOrMax != -1) ImGui.Checkbox("Auto Confirm", ref Config.InventoryConfirm);
            //     ImGui.Unindent();
            //     ImGui.PopID();
            // }

            ImGui.Checkbox("Work on Mail", ref Config.WorkOnMail);
            if (Config.WorkOnMail)
            {
                ImGui.PushID("Mail");
                ImGui.Indent();
                if (ImGui.RadioButton($"Auto fill highest amount possible", Config.MailMinOrMax == 1))
                {
                    Config.MailMinOrMax = 1;
                }
                if (ImGui.RadioButton($"Auto fill lowest amount possible", Config.MailMinOrMax == 0))
                {
                    Config.MailMinOrMax = 0;
                }
                if (ImGui.RadioButton($"Auto OK on manually entered amounts", Config.MailMinOrMax == -1))
                {
                    Config.MailMinOrMax = -1;
                }
                ImGui.Checkbox("Exclude Split Dialog", ref Config.MailExcludeSplit);
                if (Config.MailMinOrMax != -1) ImGui.Checkbox("Auto Confirm", ref Config.MailConfirm);
                ImGui.Unindent();
                ImGui.PopID();
            }
        };
    }
}
