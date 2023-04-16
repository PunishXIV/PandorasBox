using ClickLib.Clicks;
using Dalamud.Interface;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
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
            P.Ws.AddWindow(OverlayWindow);
            base.Enable();
        }

        public override void Draw()
        {
            if (Svc.GameGui.GetAddonByName("Materialize", 1) != IntPtr.Zero)
            {
                var ptr = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Materialize", 1);

                var node = ptr->UldManager.NodeList[2];

                if (node == null)
                    return;

                if (node->IsVisible)
                    node->ToggleVisibility(false);

                var position = AtkResNodeHelper.GetNodePosition(node);
                var scale = AtkResNodeHelper.GetNodeScale(node);
                var size = new Vector2(node->Width, node->Height) * scale;

                ImGuiHelpers.ForceNextWindowMainViewport();
                ImGuiHelpers.SetNextWindowPosRelativeMainViewport(position);

                ImGui.PushStyleColor(ImGuiCol.WindowBg, 0);
                float oldSize = ImGui.GetFont().Scale;
                ImGui.GetFont().Scale *= scale.X;
                ImGui.PushFont(ImGui.GetFont());
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 5f.Scale());
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(0f.Scale(), 0f.Scale()));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0f.Scale(), 0f.Scale()));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f.Scale());
                ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, size);
                ImGui.Begin($"###RepairAll{node->NodeID}", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoNavFocus
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
                        Extracting = false;
                        P.TaskManager.Abort();
                    }
                }

                ImGui.End();
                ImGui.PopStyleVar(5);
                ImGui.GetFont().Scale = oldSize;
                ImGui.PopFont();
                ImGui.PopStyleColor();

            }
        }

        private void TryExtractAll()
        {
            InventoryManager* im = InventoryManager.Instance();
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


            InventoryItem[] spiritBondedItems1 = Array.Empty<InventoryItem>();
            InventoryItem[] spiritBondedItems2 = Array.Empty<InventoryItem>();
            InventoryItem[] spiritBondedItems3 = Array.Empty<InventoryItem>();
            InventoryItem[] spiritBondedItems4 = Array.Empty<InventoryItem>();
            InventoryItem[] spiritBondedItems5 = Array.Empty<InventoryItem>();
            InventoryItem[] spiritBondedItems6 = Array.Empty<InventoryItem>();
            InventoryItem[] spiritBondedItems7 = Array.Empty<InventoryItem>();

            //Container 1
            foreach (var container in container1)
            {
                for (int i = 1; i <= container->Size; i++)
                {
                    var item = container->GetInventorySlot(i - 1);
                    if (item->Spiritbond == 10000)
                    {
                        Array.Resize(ref spiritBondedItems1, spiritBondedItems1.Length + 1);
                        spiritBondedItems1[spiritBondedItems1.Length - 1] = *item;
                    }
                }
            }

            if (spiritBondedItems1.Length > 0)
            {
                for (int i = 1; i <= spiritBondedItems1.Length; i++)
                {
                    P.TaskManager.Enqueue(() => SwitchTabs(1));
                    P.TaskManager.Enqueue(() => GenerateAndFireCallback());
                    P.TaskManager.Enqueue(() => IsMateriaMenuDialogOpen());
                    P.TaskManager.Enqueue(() => ConfirmMateriaDialog());
                    P.TaskManager.Enqueue(() => EzThrottler.Throttle("Extracted", 1000));
                    P.TaskManager.Enqueue(() => EzThrottler.Check("Extracted"));
                }
            }

            //Container 2
            foreach (var container in container2)
            {
                for (int i = 1; i <= container->Size; i++)
                {
                    var item = container->GetInventorySlot(i - 1);
                    if (item->Spiritbond == 10000)
                    {
                        Array.Resize(ref spiritBondedItems2, spiritBondedItems2.Length + 1);
                        spiritBondedItems2[spiritBondedItems2.Length - 1] = *item;
                    }
                }
            }

            if (spiritBondedItems2.Length > 0)
            {

                for (int i = 1; i <= spiritBondedItems2.Length; i++)
                {
                    P.TaskManager.Enqueue(() => SwitchTabs(2));
                    P.TaskManager.Enqueue(() => GenerateAndFireCallback());
                    P.TaskManager.Enqueue(() => IsMateriaMenuDialogOpen());
                    P.TaskManager.Enqueue(() => ConfirmMateriaDialog());
                    P.TaskManager.Enqueue(() => EzThrottler.Throttle("Extracted", 1000));
                    P.TaskManager.Enqueue(() => EzThrottler.Check("Extracted"));
                }
            }

            //Container 3
            foreach (var container in container3)
            {
                for (int i = 1; i <= container->Size; i++)
                {
                    var item = container->GetInventorySlot(i - 1);
                    if (item->Spiritbond == 10000)
                    {
                        Array.Resize(ref spiritBondedItems3, spiritBondedItems3.Length + 1);
                        spiritBondedItems3[spiritBondedItems3.Length - 1] = *item;
                    }
                }
            }

            if (spiritBondedItems3.Length > 0)
            {

                for (int i = 1; i <= spiritBondedItems3.Length; i++)
                {
                    P.TaskManager.Enqueue(() => SwitchTabs(3));
                    P.TaskManager.Enqueue(() => GenerateAndFireCallback());
                    P.TaskManager.Enqueue(() => IsMateriaMenuDialogOpen());
                    P.TaskManager.Enqueue(() => ConfirmMateriaDialog());
                    P.TaskManager.Enqueue(() => EzThrottler.Throttle("Extracted", 1000));
                    P.TaskManager.Enqueue(() => EzThrottler.Check("Extracted"));
                }
            }

            //Container 4
            foreach (var container in container4)
            {
                for (int i = 1; i <= container->Size; i++)
                {
                    var item = container->GetInventorySlot(i - 1);
                    if (item->Spiritbond == 10000)
                    {
                        Array.Resize(ref spiritBondedItems4, spiritBondedItems4.Length + 1);
                        spiritBondedItems4[spiritBondedItems4.Length - 1] = *item;
                    }
                }
            }

            if (spiritBondedItems4.Length > 0)
            {
                for (int i = 1; i <= spiritBondedItems4.Length; i++)
                {
                    P.TaskManager.Enqueue(() => SwitchTabs(4));
                    P.TaskManager.Enqueue(() => GenerateAndFireCallback());
                    P.TaskManager.Enqueue(() => IsMateriaMenuDialogOpen());
                    P.TaskManager.Enqueue(() => ConfirmMateriaDialog());
                    P.TaskManager.Enqueue(() => EzThrottler.Throttle("Extracted", 1000));
                    P.TaskManager.Enqueue(() => EzThrottler.Check("Extracted"));
                }
            }

            //Container 5
            foreach (var container in container5)
            {
                for (int i = 1; i <= container->Size; i++)
                {
                    var item = container->GetInventorySlot(i - 1);
                    if (item->Spiritbond == 10000)
                    {
                        Array.Resize(ref spiritBondedItems5, spiritBondedItems5.Length + 1);
                        spiritBondedItems5[spiritBondedItems5.Length - 1] = *item;
                    }
                }
            }

            if (spiritBondedItems5.Length > 0)
            {
                for (int i = 1; i <= spiritBondedItems5.Length; i++)
                {
                    P.TaskManager.Enqueue(() => SwitchTabs(5));
                    P.TaskManager.Enqueue(() => GenerateAndFireCallback());
                    P.TaskManager.Enqueue(() => IsMateriaMenuDialogOpen());
                    P.TaskManager.Enqueue(() => ConfirmMateriaDialog());
                    P.TaskManager.Enqueue(() => EzThrottler.Throttle("Extracted", 1000));
                    P.TaskManager.Enqueue(() => EzThrottler.Check("Extracted"));
                }
            }

            //Container 6
            foreach (var container in container6)
            {
                for (int i = 1; i <= container->Size; i++)
                {
                    var item = container->GetInventorySlot(i - 1);
                    if (item->Spiritbond == 10000)
                    {
                        Array.Resize(ref spiritBondedItems6, spiritBondedItems6.Length + 1);
                        spiritBondedItems6[spiritBondedItems6.Length - 1] = *item;
                    }
                }
            }

            if (spiritBondedItems6.Length > 0)
            {
                for (int i = 1; i <= spiritBondedItems6.Length; i++)
                {
                    P.TaskManager.Enqueue(() => SwitchTabs(6));
                    P.TaskManager.Enqueue(() => GenerateAndFireCallback());
                    P.TaskManager.Enqueue(() => IsMateriaMenuDialogOpen());
                    P.TaskManager.Enqueue(() => ConfirmMateriaDialog());
                    P.TaskManager.Enqueue(() => EzThrottler.Throttle("Extracted", 1000));
                    P.TaskManager.Enqueue(() => EzThrottler.Check("Extracted"));
                }
            }

            //Container 7
            foreach (var container in container7)
            {
                for (int i = 1; i <= container->Size; i++)
                {
                    var item = container->GetInventorySlot(i - 1);
                    if (item->Spiritbond == 10000)
                    {
                        Array.Resize(ref spiritBondedItems7, spiritBondedItems7.Length + 1);
                        spiritBondedItems7[spiritBondedItems7.Length - 1] = *item;
                    }
                }
            }

            if (spiritBondedItems7.Length > 0)
            {
                for (int i = 1; i <= spiritBondedItems7.Length; i++)
                {
                    P.TaskManager.Enqueue(() => SwitchTabs(7));
                    P.TaskManager.Enqueue(() => GenerateAndFireCallback());
                    P.TaskManager.Enqueue(() => IsMateriaMenuDialogOpen());
                    P.TaskManager.Enqueue(() => ConfirmMateriaDialog());
                    P.TaskManager.Enqueue(() => EzThrottler.Throttle("Extracted", 100));
                    P.TaskManager.Enqueue(() => EzThrottler.Check("Extracted"));
                }
            }

            P.TaskManager.Enqueue(() => { Extracting = false; return true; });
        }

        public unsafe static void CloseMateriaMenu()
        {
            if (Svc.GameGui.GetAddonByName("Materialize", 1) != IntPtr.Zero)
            {
                ActionManager.Instance()->UseAction(ActionType.General, 14);
            }
        }


        public unsafe bool? SwitchTabs(int section)
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied39]) return false;
            P.TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Switching", 300));
            P.TaskManager.EnqueueImmediate(() => EzThrottler.Check("Switching"));

            if (Svc.GameGui.GetAddonByName("Materialize", 1) != IntPtr.Zero)
            {
                var addon = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Materialize", 1);
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
                if (Svc.GameGui.GetAddonByName("Materialize") == IntPtr.Zero) return null;

                var materializePTR = Svc.GameGui.GetAddonByName("MaterializeDialog", 1);
                if (materializePTR == IntPtr.Zero)
                    return false;

                var materalizeWindow = (AtkUnitBase*)materializePTR;
                if (materalizeWindow == null)
                    return false;

                ClickMaterializeDialog.Using(materializePTR).Materialize();

                P.TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Extracting", 1000));
                P.TaskManager.EnqueueImmediate(() => EzThrottler.Check("Extracting"));

                return true;

            }
            catch
            {
                return false;
            }
        }

        public bool IsMateriaMenuDialogOpen() => Svc.GameGui.GetAddonByName("MaterializeDialog", 1) != IntPtr.Zero;

        public bool? GenerateAndFireCallback()
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.Occupied39]) return false;
            P.TaskManager.EnqueueImmediate(() => EzThrottler.Throttle("Generating", 1000));
            P.TaskManager.EnqueueImmediate(() => EzThrottler.Check("Generating"));

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

            var ptr = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Materialize", 1);
            if (ptr == null) return null;

            ptr->FireCallback(2, values);

            return true;
        }
        public override void Disable()
        {
            P.Ws.RemoveWindow(OverlayWindow);
            OverlayWindow = null;
            if (Svc.GameGui.GetAddonByName("Materialize", 1) != IntPtr.Zero)
            {
                var ptr = (AtkUnitBase*)Svc.GameGui.GetAddonByName("Materialize", 1);

                var node = ptr->UldManager.NodeList[2];

                if (node == null)
                    return;

                node->ToggleVisibility(true);
            }

            base.Disable();
        }
    }
}
