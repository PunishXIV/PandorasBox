using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.Features;
using PandorasBox.FeaturesSetup;
using PunishLib.ImGuiMethods;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using ThreadLoadImageHandler = ECommons.ImGuiMethods.ThreadLoadImageHandler;

namespace PandorasBox.UI;

internal class MainWindow : Window
{
    public OpenWindow OpenWindow { get; private set; } = OpenWindow.None;

    public MainWindow() : base($"{P.Name} {P.GetType().Assembly.GetName().Version}###PandorasBox")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public void Dispose()
    {

    }

    public override void Draw()
    {
        var region = ImGui.GetContentRegionAvail();
        var itemSpacing = ImGui.GetStyle().ItemSpacing;

        var topLeftSideHeight = region.Y;

        if (ImGui.BeginTable("$PandorasBoxTableContainer", 2, ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn($"###LeftColumn", ImGuiTableColumnFlags.WidthFixed, ImGui.GetWindowWidth() / 2);
            ImGui.TableNextColumn();

            var regionSize = ImGui.GetContentRegionAvail();
            ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f));
            if (ImGui.BeginChild($"###PandoraLeft", regionSize with { Y = topLeftSideHeight }, false, ImGuiWindowFlags.NoDecoration))
            {
                var imagePath = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "Icon.png");

                if (ThreadLoadImageHandler.TryGetTextureWrap(imagePath, out var logo))
                {
                    ImGui.Image(logo.ImGuiHandle, new(125f.Scale(), 125f.Scale()));
                }

                ImGui.Spacing();
                ImGui.Separator();

                foreach (var window in Enum.GetValues(typeof(OpenWindow)))
                {
                    if ((OpenWindow)window == OpenWindow.None) continue;

                    if (ImGui.Selectable($"{window.ToString()}", OpenWindow == (OpenWindow)window))
                    {
                        OpenWindow = (OpenWindow)window;
                    }
                }
            }
            ImGui.EndChild();
            ImGui.PopStyleVar();
            ImGui.TableNextColumn();
            if (ImGui.BeginChild($"###PandoraRight", Vector2.Zero, false, (false ? ImGuiWindowFlags.AlwaysVerticalScrollbar : ImGuiWindowFlags.None) | ImGuiWindowFlags.NoDecoration))
            {
                switch (OpenWindow)
                {
                    case OpenWindow.Actions:
                        DrawFeatures(P.Features.Where(x => x.FeatureType == FeatureType.Actions).ToArray());
                        break;
                    case OpenWindow.UI:
                        DrawFeatures(P.Features.Where(x => x.FeatureType == FeatureType.UI).ToArray());
                        break;
                    case OpenWindow.Other:
                        DrawFeatures(P.Features.Where(x => x.FeatureType == FeatureType.Other).ToArray());
                        break;
                    case OpenWindow.Targets:
                        DrawFeatures(P.Features.Where(x => x.FeatureType == FeatureType.Targeting).ToArray());
                        break;
                    case OpenWindow.About:
                        AboutTab.Draw(P);
                        break;
                }
            }
            ImGui.EndChild();

            ImGui.EndTable();
        }
    }

    private void DrawFeatures(IEnumerable<BaseFeature> features)
    {
        if (features == null || !features.Any() || features.Count() == 0) return;

        ImGuiEx.ImGuiLineCentered($"featureHeader{features.First().FeatureType}", () => ImGui.Text($"{features.First().FeatureType}"));
        ImGui.Separator();

        foreach (var feature in features)
        {
            bool enabled = feature.Enabled;
            if (ImGui.Checkbox($"{feature.Name}", ref enabled))
            {
                if (enabled)
                {
                    try
                    {
                        feature.Enable();
                        if (feature.Enabled)
                        {
                            Config.EnabledFeatures.Add(feature.GetType().Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        PluginLog.Error(ex, $"Failed to enabled {feature.Name}");
                    }
                }
                else
                {
                    try
                    {
                        feature.Disable();
                        Config.EnabledFeatures.RemoveAll(x => x == feature.GetType().Name);

                    }
                    catch (Exception ex)
                    {
                        PluginLog.Error(ex, $"Failed to enabled {feature.Name}");
                    }
                }
                Config.Save();
            }
            ImGui.TextWrapped($"{feature.Description}");
            ImGui.Separator();
        }
    }
}

public enum OpenWindow
{
    None,
    Actions,
    UI,
    Targets,
    Other,
    About
}
