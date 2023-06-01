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
    WorkshopHelper helper = new WorkshopHelper();
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
            DrawOptions();
        }
    }

    public unsafe void DrawOptions()
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

                DrawScheduleMenu();

                ImGui.End();
                ImGui.PopStyleVar(2);
            }
        }

    }

    private void DrawScheduleMenu()
    {
        if (ImGui.Button("Import Cycle Schedule From Clipboard"))
        {
            var text = ImGui.GetClipboardText();
            Dictionary<int, WorkshopHelper.Schedule> schedules = WorkshopHelper.ScheduleImport(text);
            // cannot convert error
            foreach (WorkshopHelper.Item item in WorkshopHelper.CopiedSchedule.Values)
            {
                PluginLog.Log($"item: {item.Name}");
            }
        }
        ImGui.SameLine();
        if (ImGui.BeginCombo("Cycle", ""))
        {
            for (var i = 0; i < Cycles.Count; i++)
            {
                if (ImGui.Selectable(Cycles[i], helper.Config.SelectedCycle == i + 1))
                {
                    helper.Config.SelectedCycle = i;
                }
            }

            ImGui.EndCombo();
        }

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