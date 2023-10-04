using ClickLib.Clicks;
using Dalamud.Logging;
using Dalamud.Memory;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using System;
using System.Collections.Generic;
using System.Linq;
using static ECommons.GenericHelpers;

namespace PandorasBox.Features.UI
{
    public unsafe class AutoSelectGardening : Feature
    {
        public override string Name => "Auto-select Gardening Soil/Seeds";

        public override string Description => "Automatically fill in gardening windows with seeds and soil.";

        public override FeatureType FeatureType => FeatureType.UI;

        public Dictionary<uint, Item> Seeds { get; set; }
        public Dictionary<uint, Item> Soils { get; set; }
        public Dictionary<uint, Item> Fertilizers { get; set; }

        public Dictionary<uint, Addon> AddonText { get; set; }

        public class Configs : FeatureConfig
        {
            public uint SelectedSoil = 0;
            public uint SelectedSeed = 0;

            public bool IncludeFertilzing = false;
            public uint SelectedFertilizer = 0;

            public bool AutoConfirm = false;
            public bool OnlyShowInventoryItems = false;
        }

        public Configs Config { get; private set; }

        private bool Fertilized { get; set; } = false;
        private List<int> SlotsFilled { get; set; } = new();
        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Seeds = Svc.Data.GetExcelSheet<Item>().Where(x => x.ItemUICategory.Row == 82 && x.FilterGroup == 20).ToDictionary(x => x.RowId, x => x);
            Soils = Svc.Data.GetExcelSheet<Item>().Where(x => x.ItemUICategory.Row == 82 && x.FilterGroup == 21).ToDictionary(x => x.RowId, x => x);
            Fertilizers = Svc.Data.GetExcelSheet<Item>().Where(x => x.ItemUICategory.Row == 82 && x.FilterGroup == 22).ToDictionary(x => x.RowId, x => x);
            AddonText = Svc.Data.GetExcelSheet<Addon>().ToDictionary(x => x.RowId, x => x);
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        private void RunFeature(IFramework framework)
        {
            if (Svc.ClientState.LocalPlayer == null) return;
            if (Config.IncludeFertilzing && Svc.GameGui.GetAddonByName("InventoryExpansion") != IntPtr.Zero && !Fertilized)
            {
                if (Config.SelectedFertilizer == 0) goto SoilSeeds;
                var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("InventoryExpansion");

                if (addon->IsVisible)
                {
                    if (addon->AtkValuesCount <= 5) return;
                    var fertilizeText = addon->AtkValues[5];
                    var text = MemoryHelper.ReadSeStringNullTerminated(new IntPtr(fertilizeText.String));
                    if (text.ExtractText() == AddonText[6417].Text.ExtractText())
                    {
                        var im = InventoryManager.Instance();
                        var inv1 = im->GetInventoryContainer(InventoryType.Inventory1);
                        var inv2 = im->GetInventoryContainer(InventoryType.Inventory2);
                        var inv3 = im->GetInventoryContainer(InventoryType.Inventory3);
                        var inv4 = im->GetInventoryContainer(InventoryType.Inventory4);

                        InventoryContainer*[] container =
                        {
                            inv1, inv2, inv3, inv4
                        };

                        foreach (var cont in container)
                        {
                            for (var i = 0; i < cont->Size; i++)
                            {
                                if (cont->GetInventorySlot(i)->ItemID == Config.SelectedFertilizer)
                                {
                                    var item = cont->GetInventorySlot(i);

                                    var ag = AgentInventoryContext.Instance();
                                    ag->OpenForItemSlot(cont->Type, i, AgentModule.Instance()->GetAgentByInternalId(AgentId.Inventory)->GetAddonID());
                                    var contextMenu = (AtkUnitBase*)Svc.GameGui.GetAddonByName("ContextMenu", 1);
                                    if (contextMenu == null) return;
                                    Callback.Fire(contextMenu, true, 0, 0, 0, 0, 0);
                                    Fertilized = true;
                                    return;
                                }
                            }
                        }

                        return;
                    }

                }
                else
                {
                    goto SoilSeeds;
                }
            }
            else
            {
                Fertilized = false;
            }

        SoilSeeds:
            if (Svc.GameGui.GetAddonByName("HousingGardening") != IntPtr.Zero)
            {
                if (Config.SelectedSeed == 0 && Config.SelectedSoil == 0) return;
                var invSoil = Soils.Where(x => InventoryManager.Instance()->GetInventoryItemCount(x.Value.RowId) > 0).Select(x => x.Key).ToList();
                var invSeeds = Seeds.Where(x => InventoryManager.Instance()->GetInventoryItemCount(x.Value.RowId) > 0).Select(x => x.Key).ToList();

                var im = InventoryManager.Instance();
                var inv1 = im->GetInventoryContainer(InventoryType.Inventory1);
                var inv2 = im->GetInventoryContainer(InventoryType.Inventory2);
                var inv3 = im->GetInventoryContainer(InventoryType.Inventory3);
                var inv4 = im->GetInventoryContainer(InventoryType.Inventory4);

                InventoryContainer*[] container =
                {
                            inv1, inv2, inv3, inv4
                };

                var soilIndex = 0;
                foreach (var cont in container)
                {
                    for (var i = 0; i < cont->Size; i++)
                    {
                        if (invSoil.Any(x => cont->GetInventorySlot(i)->ItemID == x))
                        {
                            var item = cont->GetInventorySlot(i);
                            if (item->ItemID == Config.SelectedSoil)
                                goto SetSeed;
                            else
                                soilIndex++;
                        }
                    }
                }

            SetSeed:
                var seedIndex = 0;
                foreach (var cont in container)
                {
                    for (var i = 0; i < cont->Size; i++)
                    {
                        if (invSeeds.Any(x => cont->GetInventorySlot(i)->ItemID == x))
                        {
                            var item = cont->GetInventorySlot(i);
                            if (item->ItemID == Config.SelectedSeed)
                                goto ClickItem;
                            else
                                seedIndex++;
                        }
                    }
                }

            ClickItem:
                var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("HousingGardening");

                if (!TaskManager.IsBusy)
                {
                    if (soilIndex != -1)
                    {
                        if (SlotsFilled.Contains(1)) TaskManager.Abort();
                        if (SlotsFilled.Contains(1)) return;
                        TaskManager.DelayNext($"Gardening1", 100);
                        TaskManager.Enqueue(() => TryClickItem(addon, 1, soilIndex));
                    }

                    if (seedIndex != -1)
                    {
                        if (SlotsFilled.Contains(2)) TaskManager.Abort();
                        if (SlotsFilled.Contains(2)) return;
                        TaskManager.DelayNext($"Gardening2", 100);
                        TaskManager.Enqueue(() => TryClickItem(addon, 2, seedIndex));
                    }

                    if (Config.AutoConfirm)
                    {
                        TaskManager.DelayNext($"Confirming", 100);
                        TaskManager.Enqueue(() => Callback.Fire(addon, false, 0, 0, 0, 0, 0), 300, false);
                        TaskManager.Enqueue(() => ConfirmYesNo(), 300, false);
                    }
                }

            }
            else
            {
                SlotsFilled.Clear();
                TaskManager.Abort();
            }

        }

        private bool? TryClickItem(AtkUnitBase* addon, int i, int itemIndex)
        {
            if (SlotsFilled.Contains(i)) return true;

            var contextMenu = (AtkUnitBase*)Svc.GameGui.GetAddonByName("ContextIconMenu", 1);

            if (contextMenu is null || !contextMenu->IsVisible)
            {
                var slot = i - 1;

                PluginLog.Debug($"{slot}");
                var values = stackalloc AtkValue[5];
                values[0] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 2
                };
                values[1] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                    UInt = (uint)slot
                };
                values[2] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 0
                };
                values[3] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 0
                };
                values[4] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                    UInt = 1
                };

                addon->FireCallback(5, values);
                CloseItemDetail();
                return false;
            }
            else
            {
                var value = (uint)(i == 1 ? 27405 : 27451);
                var values = stackalloc AtkValue[5];
                values[0] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 0
                };
                values[1] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = itemIndex
                };
                values[2] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                    UInt = value
                };
                values[3] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                    UInt = 0
                };
                values[4] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    UInt = 0
                };


                contextMenu->FireCallback(5, values, (void*)2476827163393);
                PluginLog.Debug($"Filled slot {i}");
                SlotsFilled.Add(i);
                return true;
            }
        }

        private bool CloseItemDetail()
        {
            var itemDetail = (AtkUnitBase*)Svc.GameGui.GetAddonByName("ItemDetail", 1);
            if (itemDetail is null || !itemDetail->IsVisible) return false;

            var values = stackalloc AtkValue[1];
            values[0] = new AtkValue()
            {
                Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                Int = -1
            };

            itemDetail->FireCallback(1, values);
            return true;
        }

        internal static bool ConfirmYesNo()
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied39]) return false;
            var hg = (AtkUnitBase*)Svc.GameGui.GetAddonByName("HousingGardening");
            if (hg == null) return false;

            if (hg->IsVisible && TryGetAddonByName<AddonSelectYesno>("SelectYesno", out var addon) &&
                addon->AtkUnitBase.IsVisible &&
                addon->YesButton->IsEnabled &&
                addon->AtkUnitBase.UldManager.NodeList[15]->IsVisible)
            {
                new ClickSelectYesNo((IntPtr)addon).Yes();
                return true;
            }

            return false;
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Seeds = null;
            Soils = null;
            AddonText = null;
            Fertilizers = null;
            Svc.Framework.Update -= RunFeature;
            SlotsFilled.Clear();
            base.Disable();
        }

        protected override DrawConfigDelegate DrawConfigTree => (ref bool _) =>
        {
            bool hasChanged = false;

            if (ImGui.Checkbox("Show Only Inventory Items", ref Config.OnlyShowInventoryItems))
                hasChanged = true;

            var invSoil = Config.OnlyShowInventoryItems ? Soils.Where(x => InventoryManager.Instance()->GetInventoryItemCount(x.Value.RowId) > 0).ToArray() : Soils.ToArray();
            var invSeeds = Config.OnlyShowInventoryItems ? Seeds.Where(x => InventoryManager.Instance()->GetInventoryItemCount(x.Value.RowId) > 0).ToArray() : Seeds.ToArray();
            var invFert = Config.OnlyShowInventoryItems ? Fertilizers.Where(x => InventoryManager.Instance()->GetInventoryItemCount(x.Value.RowId) > 0).ToArray() : Fertilizers.ToArray();

            var soilPrev = Config.SelectedSoil == 0 ? "" : Soils[Config.SelectedSoil].Name.ExtractText();
            if (ImGui.BeginCombo("Soil", soilPrev))
            {
                if (ImGui.Selectable("", Config.SelectedSoil == 0))
                {
                    Config.SelectedSoil = 0;
                    hasChanged = true;
                }
                foreach (var soil in invSoil)
                {
                    var selected = ImGui.Selectable(soil.Value.Name.ExtractText(), Config.SelectedSoil == soil.Key);

                    if (selected)
                    {
                        Config.SelectedSoil = soil.Key;
                        hasChanged = true;
                    }
                }

                ImGui.EndCombo();
            }

            var seedPrev = Config.SelectedSeed == 0 ? "" : Seeds[Config.SelectedSeed].Name.ExtractText();
            if (ImGui.BeginCombo("Seed", seedPrev))
            {
                if (ImGui.Selectable("", Config.SelectedSeed == 0))
                {
                    Config.SelectedSeed = 0;
                    hasChanged = true;
                }
                foreach (var seed in invSeeds)
                {
                    var selected = ImGui.Selectable(seed.Value.Name.ExtractText(), Config.SelectedSeed == seed.Key);

                    if (selected)
                    {
                        Config.SelectedSeed = seed.Key;
                        hasChanged = true;
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.Checkbox("Include Fertilizing", ref Config.IncludeFertilzing);

            if (Config.IncludeFertilzing)
            {
                var fertPrev = Config.SelectedFertilizer == 0 ? "" : Fertilizers[Config.SelectedFertilizer].Name.ExtractText();
                if (ImGui.BeginCombo("Fertilizer", fertPrev))
                {
                    if (ImGui.Selectable("", Config.SelectedFertilizer == 0))
                    {
                        Config.SelectedFertilizer = 0;
                        hasChanged = true;
                    }
                    foreach (var fert in invFert)
                    {
                        var selected = ImGui.Selectable(fert.Value.Name.ExtractText(), Config.SelectedFertilizer == fert.Key);

                        if (selected)
                        {
                            Config.SelectedFertilizer = fert.Key;
                            hasChanged = true;
                        }
                    }

                    ImGui.EndCombo();
                }
            }

            if (ImGui.Checkbox("Auto Confirm", ref Config.AutoConfirm))
                hasChanged = true;

            if (hasChanged)
                SaveConfig(Config);
        };
    }
}
