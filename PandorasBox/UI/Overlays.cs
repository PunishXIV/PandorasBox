using Dalamud.Interface.Windowing;
using ImGuiNET;
using PandorasBox.Features;
using System.Linq;

namespace PandorasBox.UI
{
    internal class Overlays : Window
    {
        private Feature Feature { get; set; }
        public Overlays(Feature t) : base($"###Overlay{t.Name}", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysUseWindowPadding | ImGuiWindowFlags.AlwaysAutoResize, true)
        {
            this.Position = new System.Numerics.Vector2(0, 0);
            Feature = t;
            IsOpen = true;
            ShowCloseButton = false;
            RespectCloseHotkey = false;
            DisableWindowSounds = true;
            this.SizeConstraints = new WindowSizeConstraints()
            {
                MaximumSize = new System.Numerics.Vector2(0, 0),
            };
            if (P.Ws.Windows.Any(x => x.WindowName == this.WindowName))
            {
                P.Ws.RemoveWindow(P.Ws.Windows.First(x => x.WindowName == this.WindowName));
            }
            P.Ws.AddWindow(this);
        }

        public override void Draw() => Feature.Draw();

        public override bool DrawConditions() => Feature.Enabled;
    }
}
