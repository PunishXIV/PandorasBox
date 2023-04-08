using Dalamud.Interface.Windowing;
using ImGuiNET;
using PandorasBox.Features;

namespace PandorasBox.UI
{
    internal class Overlays : Window
    {
        Feature Feature;
        public Overlays(Feature t) : base($"###Overlay{t.Name}", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar
                    | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoNavFocus
                    | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoSavedSettings)
        {
            Feature = t;
            IsOpen = true;
            ShowCloseButton = false;
            RespectCloseHotkey = false;
        }

        public override void Draw() => Feature.Draw();

        public override bool DrawConditions() => Feature.Enabled;
    }
}
