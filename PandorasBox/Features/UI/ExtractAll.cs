using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.Throttlers;
using ECommons.UIHelpers.AddonMasterImplementations;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Bindings.ImGui;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using PandorasBox.UI;
using System;
using System.Numerics;

namespace PandorasBox.Features.UI
{
    public unsafe class ExtractAll : Feature
    {
        public override string Name => "Extract All Materia";

        public override string Description => "Adds a button to the extract materia window to start extracting all.";

        public override FeatureType FeatureType => FeatureType.UI;

        internal Overlays OverlayWindow;

        internal bool Extracting = false;
        public override void Enable()
        {
            OverlayWindow = new(this);
            base.Enable();
        }

        public override bool DrawConditions()
        {
            return Svc.GameGui.GetAddonByName("Materialize", 1) != IntPtr.Zero;
        }

        public override void Draw()
        {
            try
            {
                if (Svc.GameGui.GetAddonByName("Materialize", 1) != IntPtr.Zero)
                {
                    var ptr = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Materialize", 1).Address;
                    if (!ptr->IsVisible)
                        return;

                    var node = ptr->UldManager.NodeList[2];

                    if (node == null)
                        return;

                    if (node->IsVisible())
                        node->ToggleVisibility(false);

                    var position = AtkResNodeHelper.GetNodePosition(node);
                    var scale = AtkResNodeHelper.GetNodeScale(node);
                    var size = new Vector2(node->Width, node->Height) * scale;

                    ImGuiHelpers.ForceNextWindowMainViewport();
                    ImGuiHelpers.SetNextWindowPosRelativeMainViewport(position);

                    ImGui.PushStyleColor(ImGuiCol.WindowBg, 0);
                    var oldSize = ImGui.GetFont().Scale;
                    ImGui.GetFont().Scale *= scale.X;
                    ImGui.PushFont(ImGui.GetFont());
                    ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 5f.Scale());
                    ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0f.Scale(), 0f.Scale()));
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0f.Scale(), 0f.Scale()));
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f.Scale());
                    ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, size);
                    ImGui.Begin($"###RepairAll{node->NodeId}", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoNavFocus
                        | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings);


                    if (!Extracting)
                    {
                        if (ImGui.Button($"Extract All###StartExtract", size))
                        {
                            Extracting = true;
                            TryExtractAll();
                        }
                    }
                    else
                    {
                        if (ImGui.Button($"Extracting. Click to abort.###AbortExtract", size))
                        {
                            Abort();
                        }
                    }

                    ImGui.End();
                    ImGui.PopStyleVar(5);
                    ImGui.GetFont().Scale = oldSize;
                    ImGui.PopFont();
                    ImGui.PopStyleColor();

                }
                else
                {
                    Abort();
                }
            }
            catch (Exception e)
            {
                Svc.Log.Debug(e, "ExtractAllException");
            }
        }

        private void Abort()
        {
            Extracting = false;
            TaskManager.Abort();
            TaskManager.Enqueue(() => YesAlready.Unlock());
        }

        private void TryExtractAll()
        {
            var im = InventoryManager.Instance();
            var inv1 = im->GetInventoryContainer(InventoryType.Inventory1);
            var inv2 = im->GetInventoryContainer(InventoryType.Inventory2);
            var inv3 = im->GetInventoryContainer(InventoryType.Inventory3);
            var inv4 = im->GetInventoryContainer(InventoryType.Inventory4);

            var arm1 = im->GetInventoryContainer(InventoryType.ArmoryOffHand);
            var arm2 = im->GetInventoryContainer(InventoryType.ArmoryHead);
            var arm3 = im->GetInventoryContainer(InventoryType.ArmoryBody);
            var arm4 = im->GetInventoryContainer(InventoryType.ArmoryHands);
            var arm5 = im->GetInventoryContainer(InventoryType.ArmoryWaist);
            var arm6 = im->GetInventoryContainer(InventoryType.ArmoryLegs);
            var arm7 = im->GetInventoryContainer(InventoryType.ArmoryFeets);
            var arm8 = im->GetInventoryContainer(InventoryType.ArmoryEar);
            var arm9 = im->GetInventoryContainer(InventoryType.ArmoryNeck);
            var arm10 = im->GetInventoryContainer(InventoryType.ArmoryWrist);
            var arm11 = im->GetInventoryContainer(InventoryType.ArmoryRings);
            var arm12 = im->GetInventoryContainer(InventoryType.ArmoryMainHand);

            var equip = im->GetInventoryContainer(InventoryType.EquippedItems);

            InventoryContainer*[] container1 =
            {
                equip
            };

            InventoryContainer*[] container2 =
            {
                arm1, arm12
            };

            InventoryContainer*[] container3 =
            {
                arm2, arm3, arm4
            };

            InventoryContainer*[] container4 =
            {
                arm6, arm7,
            };

            InventoryContainer*[] container5 =
            {
                arm8, arm9
            };

            InventoryContainer*[] container6 =
            {
                arm10, arm11
            };

            InventoryContainer*[] container7 =
            {
                inv1, inv2, inv3, inv4
            };


            var spiritBondedItems1 = Array.Empty<InventoryItem>();
            var spiritBondedItems2 = Array.Empty<InventoryItem>();
            var spiritBondedItems3 = Array.Empty<InventoryItem>();
            var spiritBondedItems4 = Array.Empty<InventoryItem>();
            var spiritBondedItems5 = Array.Empty<InventoryItem>();
            var spiritBondedItems6 = Array.Empty<InventoryItem>();
            var spiritBondedItems7 = Array.Empty<InventoryItem>();

            TaskManager.Enqueue(() => YesAlready.Lock(), "LockYesAlready");
            //Container 1
            foreach (var container in container1)
            {
                for (var i = 1; i <= container->Size; i++)
                {
                    var item = container->GetInventorySlot(i - 1);
                    if (item->SpiritbondOrCollectability == 10000)
                    {
                        Array.Resize(ref spiritBondedItems1, spiritBondedItems1.Length + 1);
                        spiritBondedItems1[^1] = *item;
                    }
                }
            }

            if (spiritBondedItems1.Length > 0)
            {
                for (var i = 1; i <= spiritBondedItems1.Length; i++)
                {
                    TaskManager.Enqueue(() => SwitchTabs(1), "SwitchTabs");
                    TaskManager.Enqueue(() => GenerateAndFireCallback(), "GenerateAndFireCallback");
                    //TaskManager.Enqueue(() => IsMateriaMenuDialogOpen(), "IsMateriaMenuDialogOpen");
                    TaskManager.Enqueue(() => ConfirmMateriaDialog(), "ConfirmMateriaDialog");
                }
            }

            //Container 2
            foreach (var container in container2)
            {
                for (var i = 1; i <= container->Size; i++)
                {
                    var item = container->GetInventorySlot(i - 1);
                    if (item->SpiritbondOrCollectability == 10000)
                    {
                        Array.Resize(ref spiritBondedItems2, spiritBondedItems2.Length + 1);
                        spiritBondedItems2[^1] = *item;
                    }
                }
            }

            if (spiritBondedItems2.Length > 0)
            {

                for (var i = 1; i <= spiritBondedItems2.Length; i++)
                {
                    TaskManager.Enqueue(() => SwitchTabs(2), "SwitchTabs");
                    TaskManager.Enqueue(() => GenerateAndFireCallback(), "GenerateAndFireCallback");
                    //TaskManager.Enqueue(() => IsMateriaMenuDialogOpen(), "IsMateriaMenuDialogOpen");
                    TaskManager.Enqueue(() => ConfirmMateriaDialog(), "ConfirmMateriaDialog");
                }
            }

            //Container 3
            foreach (var container in container3)
            {
                for (var i = 1; i <= container->Size; i++)
                {
                    var item = container->GetInventorySlot(i - 1);
                    if (item->SpiritbondOrCollectability == 10000)
                    {
                        Array.Resize(ref spiritBondedItems3, spiritBondedItems3.Length + 1);
                        spiritBondedItems3[^1] = *item;
                    }
                }
            }

            if (spiritBondedItems3.Length > 0)
            {

                for (var i = 1; i <= spiritBondedItems3.Length; i++)
                {
                    TaskManager.Enqueue(() => SwitchTabs(3), "SwitchTabs");
                    TaskManager.Enqueue(() => GenerateAndFireCallback(), "GenerateAndFireCallback");
                    //TaskManager.Enqueue(() => IsMateriaMenuDialogOpen(), "IsMateriaMenuDialogOpen");
                    TaskManager.Enqueue(() => ConfirmMateriaDialog(), "ConfirmMateriaDialog");
                }
            }

            //Container 4
            foreach (var container in container4)
            {
                for (var i = 1; i <= container->Size; i++)
                {
                    var item = container->GetInventorySlot(i - 1);
                    if (item->SpiritbondOrCollectability == 10000)
                    {
                        Array.Resize(ref spiritBondedItems4, spiritBondedItems4.Length + 1);
                        spiritBondedItems4[^1] = *item;
                    }
                }
            }

            if (spiritBondedItems4.Length > 0)
            {
                for (var i = 1; i <= spiritBondedItems4.Length; i++)
                {
                    TaskManager.Enqueue(() => SwitchTabs(4), "SwitchTabs");
                    TaskManager.Enqueue(() => GenerateAndFireCallback(), "GenerateAndFireCallback");
                    //TaskManager.Enqueue(() => IsMateriaMenuDialogOpen(), "IsMateriaMenuDialogOpen");
                    TaskManager.Enqueue(() => ConfirmMateriaDialog(), "ConfirmMateriaDialog");
                }
            }

            //Container 5
            foreach (var container in container5)
            {
                for (var i = 1; i <= container->Size; i++)
                {
                    var item = container->GetInventorySlot(i - 1);
                    if (item->SpiritbondOrCollectability == 10000)
                    {
                        Array.Resize(ref spiritBondedItems5, spiritBondedItems5.Length + 1);
                        spiritBondedItems5[^1] = *item;
                    }
                }
            }

            if (spiritBondedItems5.Length > 0)
            {
                for (var i = 1; i <= spiritBondedItems5.Length; i++)
                {
                    TaskManager.Enqueue(() => SwitchTabs(5), "SwitchTabs");
                    TaskManager.Enqueue(() => GenerateAndFireCallback(), "GenerateAndFireCallback");
                    //TaskManager.Enqueue(() => IsMateriaMenuDialogOpen(), "IsMateriaMenuDialogOpen");
                    TaskManager.Enqueue(() => ConfirmMateriaDialog(), "ConfirmMateriaDialog");
                }
            }

            //Container 6
            foreach (var container in container6)
            {
                for (var i = 1; i <= container->Size; i++)
                {
                    var item = container->GetInventorySlot(i - 1);
                    if (item->SpiritbondOrCollectability == 10000)
                    {
                        Array.Resize(ref spiritBondedItems6, spiritBondedItems6.Length + 1);
                        spiritBondedItems6[^1] = *item;
                    }
                }
            }

            if (spiritBondedItems6.Length > 0)
            {
                for (var i = 1; i <= spiritBondedItems6.Length; i++)
                {
                    TaskManager.Enqueue(() => SwitchTabs(6), "SwitchTabs");
                    TaskManager.Enqueue(() => GenerateAndFireCallback(), "GenerateAndFireCallback");
                    //TaskManager.Enqueue(() => IsMateriaMenuDialogOpen(), "IsMateriaMenuDialogOpen");
                    TaskManager.Enqueue(() => ConfirmMateriaDialog(), "ConfirmMateriaDialog");
                }
            }

            //Container 7
            foreach (var container in container7)
            {
                for (var i = 1; i <= container->Size; i++)
                {
                    var item = container->GetInventorySlot(i - 1);
                    if (item->SpiritbondOrCollectability == 10000)
                    {
                        Array.Resize(ref spiritBondedItems7, spiritBondedItems7.Length + 1);
                        spiritBondedItems7[^1] = *item;
                    }
                }
            }

            if (spiritBondedItems7.Length > 0)
            {
                for (var i = 1; i <= spiritBondedItems7.Length; i++)
                {
                    TaskManager.Enqueue(() => SwitchTabs(7), "SwitchTabs");
                    TaskManager.Enqueue(() => GenerateAndFireCallback(), "GenerateAndFireCallback");
                    //TaskManager.Enqueue(() => IsMateriaMenuDialogOpen(), "IsMateriaMenuDialogOpen");
                    TaskManager.Enqueue(() => ConfirmMateriaDialog(), "ConfirmMateriaDialog");
                }
            }
            TaskManager.Enqueue(() => Abort());
        }

        public static unsafe void CloseMateriaMenu()
        {
            if (Svc.GameGui.GetAddonByName("Materialize", 1) != IntPtr.Zero)
            {
                ActionManager.Instance()->UseAction(ActionType.GeneralAction, 14);
            }
        }


        public unsafe bool? SwitchTabs(int section)
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied39]) return false;
            TaskManager.InsertMulti([new(() => EzThrottler.Throttle("Switching", 300)), new(() => EzThrottler.Check("Switching"))]);

            if (Svc.GameGui.GetAddonByName("Materialize", 1) != IntPtr.Zero)
            {
                var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Materialize", 1).Address;
                var values = stackalloc AtkValue[2];
                values[0] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = 1
                };
                values[1] = new AtkValue()
                {
                    Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                    Int = section - 1
                };
                addon->FireCallback(2, values);

                return true;

            }
            else
            {
                return false;
            }
        }

        public unsafe bool? ConfirmMateriaDialog()
        {
            try
            {
                if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied39]) return false;
                if (Svc.GameGui.GetAddonByName("Materialize") == IntPtr.Zero) return true;

                var materializePTR = Svc.GameGui.GetAddonByName("MaterializeDialog", 1);
                if (materializePTR == IntPtr.Zero)
                    return true;

                var materalizeWindow = (AtkUnitBase*)materializePTR.Address;
                if (materalizeWindow == null)
                    return true;

                new AddonMaster.MaterializeDialog(materializePTR).Materialize();

                TaskManager.InsertMulti([new(() => EzThrottler.Throttle("Extracting", 100), "ExtractingThrottle"), new(() => EzThrottler.Check("Extracting"), "ExtractingCheck")]);
                return true;

            }
            catch
            {
                return false;
            }
        }

        public static bool IsMateriaMenuDialogOpen() => Svc.GameGui.GetAddonByName("MaterializeDialog", 1) != IntPtr.Zero;

        public bool? GenerateAndFireCallback()
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied39]) return false;
            TaskManager.InsertMulti([new(() => EzThrottler.Throttle("Generating", 100), "Generating"), new(() => EzThrottler.Check("Generating"), "GeneratingCheck")]);

            var values = stackalloc AtkValue[2];
            values[0] = new()
            {
                Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.Int,
                Int = 2,
            };
            values[1] = new()
            {
                Type = FFXIVClientStructs.FFXIV.Component.GUI.ValueType.UInt,
                UInt = 0,
            };

            var ptr = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Materialize", 1).Address;
            if (ptr == null) return true;

            ptr->FireCallback(2, values);

            return true;
        }
        public override void Disable()
        {
            P.Ws.RemoveWindow(OverlayWindow);
            OverlayWindow = null!;
            if (Svc.GameGui.GetAddonByName("Materialize", 1) != IntPtr.Zero)
            {
                var ptr = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Materialize", 1).Address;

                var node = ptr->UldManager.NodeList[2];

                if (node == null)
                    return;

                node->ToggleVisibility(true);
            }
            base.Disable();
        }
    }
}
