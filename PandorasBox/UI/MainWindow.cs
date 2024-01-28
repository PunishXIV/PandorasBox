using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Utility;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ImGuiMethods;
using ImGuiNET;
using PandorasBox.Features;
using PandorasBox.Features.ChatFeature;
using PandorasBox.FeaturesSetup;
using PandorasBox.IPC;
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

    public bool ThemePushed = false;

    public MainWindow() : base($"{P.Name} {P.GetType().Assembly.GetName().Version}###PandorasBox")
    {
        this.SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        RespectCloseHotkey = false;
    }

    public static void Dispose()
    {

    }


    public override void PreDraw()
    {
        if (!Config.DisabledTheme && !ThemePushed)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 1, 1));
            ImGui.PushStyleColor(ImGuiCol.TextDisabled, new Vector4(0.5f, 0.5f, 0.5f, 1));
            ImGui.PushStyleColor(ImGuiCol.WindowBg, new Vector4(0.03f, 0.17f, 0.04f, 0.94f));
            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0, 0, 0, 0));
            ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(0.08f, 0.08f, 0.08f, 0.94f));
            ImGui.PushStyleColor(ImGuiCol.Border, new Vector4(0.38f, 1, 0, 0.5f));
            ImGui.PushStyleColor(ImGuiCol.BorderShadow, new Vector4(0.01f, 0.13f, 0, 0.63f));
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.17f, 0.48f, 0.16f, 0.54f));
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, new Vector4(0.26f, 0.98f, 0.32f, 0.4f));
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, new Vector4(0.26f, 0.98f, 0.17f, 0.67f));
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.01f, 0.07f, 0.01f, 1f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0, 0.56f, 0.09f, 0.51f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgCollapsed, new Vector4(0, 0.56f, 0.09f, 0.51f));
            ImGui.PushStyleColor(ImGuiCol.MenuBarBg, new Vector4(0, 0.29f, 0.68f, 1));
            ImGui.PushStyleColor(ImGuiCol.ScrollbarBg, new Vector4(0, 0.15f, 0, 0.53f));
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrab, new Vector4(0.1f, 0.41f, 0.06f, 1));
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabHovered, new Vector4(0, 0.66f, 0.04f, 1));
            ImGui.PushStyleColor(ImGuiCol.ScrollbarGrabActive, new Vector4(0.04f, 0.87f, 0, 1f));
            ImGui.PushStyleColor(ImGuiCol.CheckMark, new Vector4(0.26f, 0.98f, 0.4f, 1f));
            ImGui.PushStyleColor(ImGuiCol.SliderGrab, new Vector4(0.21f, 0.61f, 0f, 1f));
            ImGui.PushStyleColor(ImGuiCol.SliderGrabActive, new Vector4(0.36f, 0.87f, .22f, 1));
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0.6f, 0.05f, 0.4f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(.2f, 78f, .32f, 1));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0, .57f, .07f, 1));
            ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(.12f, .82f, .28f, .31f));
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0, 0.74f, .11f, .8f));
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, new Vector4(.09f, .69f, .04f, 1));
            ImGui.PushStyleColor(ImGuiCol.Separator, new Vector4(0.09f, 0.67f, 0.01f, 0.50f));
            ImGui.PushStyleColor(ImGuiCol.SeparatorHovered, new Vector4(0.32f, 0.75f, 0.10f, 0.78f));
            ImGui.PushStyleColor(ImGuiCol.SeparatorActive, new Vector4(0.10f, 0.75f, 0.11f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.ResizeGrip, new Vector4(0.32f, 0.98f, 0.26f, 0.20f));
            ImGui.PushStyleColor(ImGuiCol.ResizeGripHovered, new Vector4(0.26f, 0.98f, 0.28f, 0.67f));
            ImGui.PushStyleColor(ImGuiCol.ResizeGripActive, new Vector4(0.22f, 0.69f, 0.06f, 0.95f));
            ImGui.PushStyleColor(ImGuiCol.Tab, new Vector4(0.18f, 0.58f, 0.18f, 0.86f));
            ImGui.PushStyleColor(ImGuiCol.TabHovered, new Vector4(0.26f, 0.98f, 0.28f, 0.80f));
            ImGui.PushStyleColor(ImGuiCol.TabActive, new Vector4(0.20f, 0.68f, 0.24f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.TabUnfocused, new Vector4(0.07f, 0.15f, 0.08f, 0.97f));
            ImGui.PushStyleColor(ImGuiCol.TabUnfocusedActive, new Vector4(0.14f, 0.42f, 0.19f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.PlotLines, new Vector4(0.61f, 0.61f, 0.61f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.PlotHistogramHovered, new Vector4(1.00f, 0.43f, 0.35f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, new Vector4(0.90f, 0.70f, 0.00f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.PlotHistogramHovered, new Vector4(1.00f, 0.60f, 0.00f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.TableHeaderBg, new Vector4(0.19f, 0.19f, 0.20f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.TableBorderStrong, new Vector4(0.31f, 0.31f, 0.35f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.TableBorderLight, new Vector4(0.23f, 0.23f, 0.25f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.TableRowBg, new Vector4(0.00f, 0.00f, 0.00f, 0.00f));
            ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, new Vector4(1.00f, 1.00f, 1.00f, 0.06f));
            ImGui.PushStyleColor(ImGuiCol.TextSelectedBg, new Vector4(0.00f, 0.89f, 0.20f, 0.35f));
            ImGui.PushStyleColor(ImGuiCol.DragDropTarget, new Vector4(1.00f, 1.00f, 0.00f, 0.90f));
            ImGui.PushStyleColor(ImGuiCol.NavHighlight, new Vector4(0.26f, 0.98f, 0.35f, 1.00f));
            ImGui.PushStyleColor(ImGuiCol.NavWindowingHighlight, new Vector4(1.00f, 1.00f, 1.00f, 0.70f));
            ImGui.PushStyleColor(ImGuiCol.NavWindowingDimBg, new Vector4(0.80f, 0.80f, 0.80f, 0.20f));
            ImGui.PushStyleColor(ImGuiCol.ModalWindowDimBg, new Vector4(0.80f, 0.80f, 0.80f, 0.35f));

            ThemePushed = true;

        }
        base.PreDraw();
    }

    public override void PostDraw()
    {
        if (ThemePushed)
        {
            ImGui.PopStyleColor(52);
            ThemePushed = false;
        }
        base.PostDraw();
    }

    private string searchString = string.Empty;
    private List<BaseFeature> FilteredFeatures = new();
    private bool hornybonk;

    public override void Draw()
    {
        var region = ImGui.GetContentRegionAvail();
        var itemSpacing = ImGui.GetStyle().ItemSpacing;

        var topLeftSideHeight = region.Y;

        if (ImGui.BeginTable("$PandorasBoxTableContainer", 2, ImGuiTableFlags.Resizable))
        {
            try
            {
                ImGui.TableSetupColumn($"###LeftColumn", ImGuiTableColumnFlags.WidthFixed, ImGui.GetWindowWidth() / 2);
                ImGui.TableNextColumn();

                var regionSize = ImGui.GetContentRegionAvail();
                ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f));
                if (ImGui.BeginChild($"###PandoraLeft", regionSize with { Y = topLeftSideHeight }, false, ImGuiWindowFlags.NoDecoration))
                {
                    var imagePath = Config.DisabledTheme ? Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "pandora.png") : Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName!, "pandora_g.png");

                    if (ThreadLoadImageHandler.TryGetTextureWrap(imagePath, out var logo))
                    {
                        ImGuiEx.ImGuiLineCentered("###Logo", () => { ImGui.Image(logo.ImGuiHandle, new(125f.Scale(), 125f.Scale())); });

                    }

                    ImGui.Spacing();
                    ImGui.Separator();

                    foreach (var window in Enum.GetValues(typeof(OpenWindow)))
                    {
                        if ((OpenWindow)window == OpenWindow.None) continue;

                        if (ImGui.Selectable($"{window}", OpenWindow == (OpenWindow)window))
                        {
                            OpenWindow = (OpenWindow)window;
                        }
                    }

                    ImGui.Spacing();
                    if (Config.DisabledTheme)
                    {
                        if (ImGui.Selectable("Enable Theme", false))
                        {
                            Config.DisabledTheme = false;
                            Config.Save();
                        }
                    }
                    else
                    {
                        if (ImGui.Selectable("Disable Theme", false))
                        {
                            Config.DisabledTheme = true;
                            Config.Save();
                        }
                    }

                    ImGui.SetCursorPosY(ImGui.GetContentRegionMax().Y - 45f);
                    ImGuiEx.ImGuiLineCentered("###Search", () => { ImGui.Text($"Search"); ImGuiComponents.HelpMarker("Searches feature names and descriptions for a given word or phrase."); });
                    ImGuiEx.SetNextItemFullWidth();
                    if (ImGui.InputText("###FeatureSearch", ref searchString, 500))
                    {
                        if (searchString.Equals("ERP", StringComparison.CurrentCultureIgnoreCase) && !hornybonk)
                        {
                            hornybonk = true;
                            Util.OpenLink("https://www.youtube.com/watch?v=oO-gc3Lh-oI");
                        }
                        else
                        {
                            hornybonk = false;
                        }
                        FilteredFeatures.Clear();
                        if (searchString.Length > 0)
                        {
                            foreach (var feature in P.Features)
                            {
                                if (feature.FeatureType == FeatureType.Commands || feature.FeatureType == FeatureType.Disabled) continue;

                                if (feature.Description.Contains(searchString, StringComparison.CurrentCultureIgnoreCase) ||
                                    feature.Name.Contains(searchString, StringComparison.CurrentCultureIgnoreCase))
                                    FilteredFeatures.Add(feature);
                            }
                        }
                    }

                }
                ImGui.EndChild();
                ImGui.PopStyleVar();
                ImGui.TableNextColumn();
                if (ImGui.BeginChild($"###PandoraRight", Vector2.Zero, false, (false ? ImGuiWindowFlags.AlwaysVerticalScrollbar : ImGuiWindowFlags.None) | ImGuiWindowFlags.NoDecoration))
                {
                    if (FilteredFeatures.Count() > 0)
                    {
                        DrawFeatures(FilteredFeatures.ToArray());
                    }
                    else
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
                            case OpenWindow.Chat:
                                DrawFeatures(P.Features.Where(x => x.FeatureType == FeatureType.ChatFeature).ToArray());
                                break;
                            case OpenWindow.Commands:
                                DrawCommands(P.Features.Where(x => x.FeatureType == FeatureType.Commands).ToArray());
                                break;
                            case OpenWindow.About:
                                AboutTab.Draw("Pandora's Box");
                                break;
                        }
                    }
                }
                ImGui.EndChild();
            }
            catch(Exception ex)
            {
                ex.Log();
                ImGui.EndTable();
            }
            ImGui.EndTable();
        }
    }

    private static void DrawCommands(BaseFeature[] features)
    {
        if (features == null || !features.Any() || features.Length == 0) return;
        ImGuiEx.ImGuiLineCentered($"featureHeader{features.First().FeatureType}", () => ImGui.Text($"{features.First().FeatureType}"));
        ImGui.Separator();

        if (ImGui.BeginTable("###CommandsTable", 5, ImGuiTableFlags.Borders))
        {
            ImGui.TableSetupColumn("Name");
            ImGui.TableSetupColumn("Command");
            ImGui.TableSetupColumn("Parameters");
            ImGui.TableSetupColumn("Description");
            ImGui.TableSetupColumn("Aliases");

            ImGui.TableHeadersRow();
            foreach (var feature in features.Cast<CommandFeature>())
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.TextWrapped(feature.Name);
                ImGui.TableNextColumn();
                ImGui.TextWrapped(feature.Command);
                ImGui.TableNextColumn();
                ImGui.TextWrapped(string.Join(", ", feature.Parameters));
                ImGui.TableNextColumn();
                ImGui.TextWrapped($"{feature.Description}");
                ImGui.TableNextColumn();
                ImGui.TextWrapped($"{string.Join(", ", feature.Alias)}");

            }

            ImGui.EndTable();
        }
    }

    private void DrawFeatures(IEnumerable<BaseFeature> features)
    {
        if (features == null || !features.Any() || !features.Any()) return;

        ImGuiEx.ImGuiLineCentered($"featureHeader{features.First().FeatureType}", () =>
        {
            if (FilteredFeatures.Count > 0)
            {
                ImGui.Text($"Search Results");
            }
            else
                ImGui.Text($"{features.First().FeatureType}");
        });
        ImGui.Separator();

        foreach (var feature in features)
        {
            if (feature.GetType() == typeof(AutoTPCoords) && !TeleporterIPC.IsEnabled())
                ImGui.BeginDisabled();

            var enabled = feature.Enabled;
            if (ImGui.Checkbox($"###{feature.Name}", ref enabled))
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
                        Svc.Log.Error(ex, $"Failed to enabled {feature.Name}");
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
                        Svc.Log.Error(ex, $"Failed to enabled {feature.Name}");
                    }
                }
                Config.Save();
            }
            ImGui.SameLine();
            feature.DrawConfig(ref enabled);
            ImGui.Spacing();
            ImGui.TextWrapped($"{feature.Description}");

            ImGui.Separator();

            if (feature.GetType() == typeof(AutoTPCoords) && !TeleporterIPC.IsEnabled())
                ImGui.EndDisabled();
        }
    }
}

public enum OpenWindow
{
    None,
    Actions,
    UI,
    Targets,
    Chat,
    Other,
    Commands,
    About
}
