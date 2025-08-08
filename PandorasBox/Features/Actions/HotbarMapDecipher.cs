using Dalamud.Hooking;
using Dalamud.Memory;
using ECommons.DalamudServices;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using System;
using System.Collections.Generic;
using static ECommons.GenericHelpers;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace PandorasBox.Features.Actions
{
    internal unsafe class HotbarMapDecipher : Feature
    {
        public override string Name { get; } = "Map Hotbar Decipher";
        public override string Description { get; } = "Allows deciphering treaure maps from hotbar.";
        public override FeatureType FeatureType { get; } = FeatureType.Actions;

        public new delegate bool UseActionDelegate(ActionManager* actionManager, uint actionType, uint actionID, ulong targetObjectID, uint param, uint useType, int pvp, bool* isGroundTarget);

        public new static Hook<UseActionDelegate>? UseActionHook;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Automatically Decipher")]
            public bool AutoDecipher = false;
        }

        public Configs Config { get; private set; } = null!;

        public override bool UseAutoConfig => true;
        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            UseActionHook ??= Svc.Hook.HookFromAddress<UseActionDelegate>((nint)ActionManager.Addresses.UseAction.Value, UseActionDetour);
            UseActionHook.Enable();
            base.Enable();
        }

        private bool UseActionDetour(ActionManager* actionManager, uint actionType, uint actionID, ulong targetObjectID, uint param, uint useType, int pvp, bool* isGroundTarget)
        {
            if (actionType == 2)
            {
                if (ActionManager.Instance()->GetActionStatus(ActionType.Item, actionID, Svc.ClientState.LocalContentId) != 0)
                {
                    TaskManager.Abort();
                    return UseActionHook!.Original(actionManager, actionType, actionID, targetObjectID, param, useType, pvp, isGroundTarget);
                }

                if (Svc.Data.GetExcelSheet<Item>().FindFirst(x => x.RowId == actionID, out var item) && item.FilterGroup == 18)
                {
                    TaskManager.Enqueue(() => OpenItem(actionID));

                    if (Config.AutoDecipher)
                    {
                        TaskManager.EnqueueDelay(200);
                        TaskManager.Enqueue(() => ConfirmYesNo());
                    }
                }
            }

            return UseActionHook!.Original(actionManager, actionType, actionID, targetObjectID, param, useType, pvp, isGroundTarget);
        }

        private unsafe bool? OpenItem(uint ItemId)
        {
            var invId = AgentModule.Instance()->GetAgentByInternalId(AgentId.Inventory)->GetAddonId();

            if (IsMoving())
            {
                return null;
            }

            if (!IsInventoryFree())
            {
                return null;
            }

            if (InventoryManager.Instance()->GetInventoryItemCount(ItemId) == 0)
            {
                return true;
            }

            var inventories = new List<InventoryType>
            {
                InventoryType.Inventory1,
                InventoryType.Inventory2,
                InventoryType.Inventory3,
                InventoryType.Inventory4,
            };

            foreach (var inv in inventories)
            {
                var container = InventoryManager.Instance()->GetInventoryContainer(inv);
                for (var i = 0; i < container->Size; i++)
                {
                    var item = container->GetInventorySlot(i);

                    if (item->ItemId == ItemId)
                    {
                        var ag = AgentInventoryContext.Instance();
                        ag->OpenForItemSlot(container->Type, i,0, AgentModule.Instance()->GetAgentByInternalId(AgentId.Inventory)->GetAddonId());
                        var contextMenu = (AtkUnitBase*)Svc.GameGui.GetAddonByName("ContextMenu", 1).Address;
                        if (contextMenu != null)
                        {
                            var contextAgent = AgentInventoryContext.Instance();
                            var indexDecipher = -1;

                            var loops = 0;
                            foreach (var contextObj in contextAgent->EventParams)
                            {
                                if (contextObj.Type == ValueType.String)
                                {
                                    var label = MemoryHelper.ReadSeStringNullTerminated(new IntPtr(contextObj.String));

                                    if (Svc.Data.GetExcelSheet<Addon>().GetRow(8100).Text == label.TextValue) indexDecipher = loops;

                                    loops++;
                                }
                            }

                            if (indexDecipher != -1)
                            {
                                var values = stackalloc AtkValue[5];
                                values[0] = new AtkValue()
                                {
                                    Type = ValueType.Int,
                                    Int = 0
                                };
                                values[1] = new AtkValue()
                                {
                                    Type = ValueType.Int,
                                    Int = indexDecipher,
                                };
                                values[2] = new AtkValue()
                                {
                                    Type = ValueType.Int,
                                    Int = 0
                                };
                                values[3] = new AtkValue()
                                {
                                    Type = ValueType.Int,
                                    Int = 0
                                };
                                values[4] = new AtkValue()
                                {
                                    Type = ValueType.Int,
                                    UInt = 0
                                };
                                contextMenu->FireCallback(5, values, true);
                            }

                            return true;
                        }
                    }
                }
            }

            return false;
        }

        internal static bool ConfirmYesNo()
        {
            if (TryGetAddonByName<AddonSelectYesno>("SelectYesno", out var addon) &&
                addon->AtkUnitBase.IsVisible &&
                addon->YesButton->IsEnabled &&
                addon->AtkUnitBase.UldManager.NodeList[15]->IsVisible())
            {
                new AddonMaster.SelectYesno((IntPtr)addon).Yes();
                return true;
            }

            return false;
        }

        public override void Disable()
        {
            SaveConfig(Config);
            UseActionHook?.Disable();
            base.Disable();
        }

        public override void Dispose()
        {
            UseActionHook?.Dispose();
            base.Dispose();
        }
    }
}
