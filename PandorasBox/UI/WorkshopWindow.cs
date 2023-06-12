using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using ImGuiNET;
using PandorasBox.Features;
using PandorasBox.Features.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace PandorasBox.UI;
internal class WorkshopWindow : Window
{
    WorkshopHelper WH = new WorkshopHelper();
    List<WorkshopHelper.Item> items;
    public static bool ResetPosition = false;
    public WorkshopWindow() : base($"###WorkshopWindow", ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoNavInputs)
    {
        this.Size = new Vector2(0, 0);
        this.Position = new Vector2(0, 0);
        IsOpen = true;
        ShowCloseButton = false;
        RespectCloseHotkey = false;
        this.SizeConstraints = new WindowSizeConstraints()
        {
            MaximumSize = new Vector2(0, 0),
        };
    }

    private List<string> Cycles { get; set; } = new()
    {
        "C1",
        "C2",
        "C3",
        "C4",
        "C5",
        "C6",
        "C7"
    };

    public override void Draw()
    {
        if (WorkshopHelper._enabled)
        {
            DrawSchedulerUI();
        }
    }

    public unsafe void DrawSchedulerUI()
    {
        var workshopWindow = Svc.GameGui.GetAddonByName("MJICraftSchedule", 1);
        if (workshopWindow == IntPtr.Zero)
            return;

        var addonPtr = (AtkUnitBase*)workshopWindow;
        if (addonPtr == null)
            return;

        var baseX = addonPtr->X;
        var baseY = addonPtr->Y;

        if (addonPtr->UldManager.NodeListCount > 1)
        {
            if (addonPtr->UldManager.NodeList[1]->IsVisible)
            {
                var node = addonPtr->UldManager.NodeList[1];

                if (!node->IsVisible)
                    return;

                var position = GetNodePosition(node);
                var scale = GetNodeScale(node);
                var size = new Vector2(node->Width, node->Height) * scale;
                var center = new Vector2((position.X + size.X) / 2, (position.Y - size.Y) / 2);
                //position += ImGuiHelpers.MainViewport.Pos;

                ImGuiHelpers.ForceNextWindowMainViewport();

                if ((ResetPosition && position.X != 0))
                {
                    ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(position.X + size.X + 7, position.Y + 7), ImGuiCond.Always);
                    ResetPosition = false;
                }
                else
                {
                    ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(position.X + size.X + 7, position.Y + 7), ImGuiCond.FirstUseEver);
                }

                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(7f, 7f));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(0f, 0f));
                ImGui.Begin($"###Options{node->NodeID}", ImGuiWindowFlags.NoScrollbar
                    | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.AlwaysUseWindowPadding);

                DrawSchedulerOptions();

                ImGui.End();
                ImGui.PopStyleVar(2);
            }
        }
    }

    private void DrawSchedulerOptions()
    {
        ImGui.Columns(2, "SchedulerOptionsColumns", true);

        ImGui.Text("First column");
        if (ImGui.Button("Import"))
        {
            var text = ImGui.GetClipboardText();
            List<string> rawItemStrings = text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            items = WorkshopHelper.ScheduleImport(rawItemStrings);
        }

        if (ImGui.BeginListBox("##Listbox", new Vector2(ImGui.GetColumnWidth(), 100)))
        {
            if (items != null)
            {
                foreach (WorkshopHelper.Item item in items)
                {
                    if (ImGui.Selectable(item.Name)) return;
                }
            }
            ImGui.EndListBox();
        }

        if (ImGui.Button("Fire")) { WH.ScheduleList(); }

        ImGui.NextColumn();
        ImGui.Text("Second column");

        if (ImGui.BeginCombo("Cycles", Cycles[0]))
        {
            for (int i = 0; i < Cycles.Count; i++)
            {
                bool isSelected = (Cycles[i] == Cycles[0]);
                if (ImGui.Selectable(Cycles[i], isSelected))
                    Cycles[0] = Cycles[i];

                if (isSelected)
                    ImGui.SetItemDefaultFocus();
            }
            ImGui.EndCombo();
        }
        // for (var i = 0; i < WH.Config.Workshops.Count; i++)
        // {
        //     var configValue = WH.Config.Workshops[i];
        //     if (ImGui.Checkbox($"W{i + 1}", ref configValue)) { WH.Config.Workshops[i] = configValue; }
        // }

        ImGui.Columns(1);
    }

    public static unsafe Vector2 GetNodePosition(AtkResNode* node)
    {
        var pos = new Vector2(node->X, node->Y);
        var par = node->ParentNode;
        while (par != null)
        {
            pos *= new Vector2(par->ScaleX, par->ScaleY);
            pos += new Vector2(par->X, par->Y);
            par = par->ParentNode;
        }

        return pos;
    }

    public static unsafe Vector2 GetNodeScale(AtkResNode* node)
    {
        if (node == null) return new Vector2(1, 1);
        var scale = new Vector2(node->ScaleX, node->ScaleY);
        while (node->ParentNode != null)
        {
            node = node->ParentNode;
            scale *= new Vector2(node->ScaleX, node->ScaleY);
        }

        return scale;
    }
}