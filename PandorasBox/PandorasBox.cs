using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using ECommons;
using ECommons.DalamudServices;
using PandorasBox.UI;
using PunishLib.Sponsor;

namespace PandorasBox;

public class PandorasBox : IDalamudPlugin
{
    public string Name => "Pandora's Box";
    private const string CommandName = "/pandora";
    internal Configuration Configuration { get; init; }
    internal WindowSystem Ws;
    internal MainWindow MainWindow;
    internal Configuration Config;

    internal static PandorasBox P;

    public PandorasBox(DalamudPluginInterface pluginInterface)
    {
        P = this;
        ECommonsMain.Init(pluginInterface, P);
        SponsorManager.SetSponsorInfo("https://ko-fi.com/taurenkey");
        Ws = new();
        MainWindow = new();
        Ws.AddWindow(MainWindow);

        Config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Config.Initialize(Svc.PluginInterface);

        Svc.Commands.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Help!",
            ShowInHelp = true
        });

        Svc.PluginInterface.UiBuilder.Draw += Ws.Draw;
        Svc.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;


    }

    public void Dispose()
    {
        Svc.Commands.RemoveHandler(CommandName);
        Svc.PluginInterface.UiBuilder.Draw -= Ws.Draw;
        Svc.PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
        Ws.RemoveAllWindows();
        MainWindow = null;
        Ws = null;
        ECommonsMain.Dispose();
        P = null;
    }

    private void OnCommand(string command, string args)
    {
        MainWindow.IsOpen = !MainWindow.IsOpen;
    }

    public void DrawConfigUI()
    {
        MainWindow.IsOpen = !MainWindow.IsOpen;
    }
}

