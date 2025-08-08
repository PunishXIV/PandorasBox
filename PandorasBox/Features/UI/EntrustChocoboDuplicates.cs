using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Dalamud.Bindings.ImGui;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using PandorasBox.UI;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Lumina.Excel.Sheets;
using static ECommons.UIHelpers.AddonMasterImplementations.AddonMaster;
using static FFXIVClientStructs.FFXIV.Component.GUI.AtkEventDispatcher;

namespace PandorasBox.Features.UI
{
    public unsafe class EntrustChocoboDuplicates : Feature
    {
        public override string Name => "Saddlebag Entrust Duplicates";
        public override string Description => "Adds a button to the bottom of the saddlebag to entrust duplicates.";
        public override FeatureType FeatureType => FeatureType.UI;

        private InventoryType[] playerInventory = [InventoryType.Inventory1, InventoryType.Inventory2, InventoryType.Inventory3, InventoryType.Inventory4];
        private InventoryType[] saddlebag = [InventoryType.SaddleBag1, InventoryType.SaddleBag2, InventoryType.PremiumSaddleBag1, InventoryType.PremiumSaddleBag2];

        private Overlays Overlay { get; set; }

        public override void Enable()
        {
            Overlay = new(this);
            base.Enable();
        }

        public override bool DrawConditions()
        {
            return Svc.GameGui.GetAddonByName("InventoryBuddy") != nint.Zero;
        }
        public override void Draw()
        {
            var addon = (AddonInventoryBuddy*)Svc.GameGui.GetAddonByName("InventoryBuddy").Address;
            if (addon != null && addon->AtkUnitBase.IsVisible)
            {
                var node = addon->AtkUnitBase.UldManager.NodeList[3];

                if (node == null)
                    return;

                var position = AtkResNodeHelper.GetNodePosition(node);
                var scale = AtkResNodeHelper.GetNodeScale(node);
                var size = new Vector2(node->Width, node->Height) * scale;

                ImGuiHelpers.ForceNextWindowMainViewport();
                var pos = position + size with { Y = 0 };
                pos.X += 12f;
                ImGuiHelpers.SetNextWindowPosRelativeMainViewport(pos);

                ImGui.PushStyleColor(ImGuiCol.WindowBg, 0);
                var oldSize = ImGui.GetFont().Scale;
                ImGui.GetFont().Scale *= scale.X;
                ImGui.PushFont(ImGui.GetFont());
                ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0f.Scale());
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(3f.Scale(), 3f.Scale()));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0f.Scale(), 0f.Scale()));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0f.Scale());
                ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, size);
                ImGui.Begin($"###DesynthAll{node->NodeId}", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoNavFocus
                    | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings);

                if (ImGui.Button("Entrust Duplicates"))
                {
                    var inv = InventoryManager.Instance();
                    foreach (var inventory in playerInventory)
                    {
                        var container = inv->GetInventoryContainer(inventory);
                        for (var i = 1; i <= container->Size; i++)
                        {
                            var item = container->GetInventorySlot(i - 1);
                            if (item->ItemId == 0)
                                continue;

                            foreach (var saddlebags in saddlebag)
                            {
                                var saddleContainer = inv->GetInventoryContainer(saddlebags);
                                for (var p = 1; p <= saddleContainer->Size; p++)
                                {
                                    var saddleItem = saddleContainer->GetInventorySlot(p - 1);
                                    if (saddleItem->ItemId == 0)
                                        continue;

                                    var saddleItemData = Svc.Data.GetExcelSheet<Item>().GetRow(saddleItem->ItemId);
                                    if (saddleItemData.IsUnique)
                                        continue;
                                    
                                    if (saddleItem->ItemId == item->ItemId)
                                    {
                                        uint total = (uint)(saddleItem->Quantity + item->Quantity);
                                        TaskManager.EnqueueDelay(200);
                                        TaskManager.Enqueue(() =>
                                        {
                                            FireInventoryMenu(inventory, item, 56);
                                        });
                                    }
                                }
                            }
                        }
                    }
                }

                ImGui.End();
                ImGui.PopStyleVar(5);
                ImGui.GetFont().Scale = oldSize;
                ImGui.PopFont();
                ImGui.PopStyleColor();
            }
        }

        private static void FireInventoryMenu(InventoryType inventory, InventoryItem* item, int eventId)
        {
            var ag = AgentInventoryContext.Instance();
            ag->OpenForItemSlot(inventory, item->Slot,0, AgentModule.Instance()->GetAgentByInternalId(AgentId.Inventory)->GetAddonId());
            var contextMenu = (AtkUnitBase*)Svc.GameGui.GetAddonByName("ContextMenu", 1).Address;
            if (contextMenu == null) return;

            for (int e = 0; e <= contextMenu->AtkValuesCount; e++)
            {
                if (ag->EventIds[e] == eventId)
                {
                   ECommons.Automation.Callback.Fire(contextMenu, true, 0, e - 7, 0, 0, 0);
                    return;
                }
            }
        }

        public override void Disable()
        {
            P.Ws.RemoveWindow(Overlay);
            Overlay = null;
            base.Disable();
        }
    }
}
