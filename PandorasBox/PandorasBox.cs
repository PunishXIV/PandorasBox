using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ECommons;
using ECommons.Automation.LegacyTaskManager;
using ECommons.DalamudServices;
using PandorasBox.Features;
using PandorasBox.FeaturesSetup;
using PandorasBox.IPC;
using PandorasBox.UI;
using PunishLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PandorasBox;

public class PandorasBox : IDalamudPlugin
{
    public string Name => "Pandora's Box";
    private const string CommandName = "/pandora";
    internal WindowSystem Ws;
    internal MainWindow MainWindow;

    internal static PandorasBox P;
    internal static IDalamudPluginInterface pi;
    internal static Configuration Config;

    public List<FeatureProvider> FeatureProviders = new();
    private FeatureProvider provider;
    public IEnumerable<BaseFeature> Features => FeatureProviders.Where(x => !x.Disposed).SelectMany(x => x.Features).OrderBy(x => x.Name);
    internal TaskManager TaskManager;

    public PandorasBox(IDalamudPluginInterface pluginInterface)
    {
        P = this;
        pi = pluginInterface;
        Initialize();
    }

    private void Initialize()
    {
        ECommonsMain.Init(pi, P, ECommons.Module.All);
        PunishLibMain.Init(pi, "Pandora's Box", new AboutPlugin() { Sponsor = "https://ko-fi.com/taurenkey" });

        Ws = new();
        MainWindow = new();
        Ws.AddWindow(MainWindow);
        TaskManager = new();
        Config = pi.GetPluginConfig() as Configuration ?? new Configuration();
        Config.Initialize(Svc.PluginInterface);

        Svc.Commands.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the Pandora menu.",
            ShowInHelp = true
        });

        Svc.PluginInterface.UiBuilder.Draw += Ws.Draw;
        Svc.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        Common.Setup();
        PandoraIPC.Init();
        Events.Init();
        AFKTimer.Init();
        provider = new FeatureProvider(Assembly.GetExecutingAssembly());
        provider.LoadFeatures();
        FeatureProviders.Add(provider);

    }


    public void Dispose()
    {
        Svc.Commands.RemoveHandler(CommandName);
        foreach (var f in Features.Where(x => x is not null && x.Enabled))
        {
            f.Disable();
            f.Dispose();
        }

        provider.UnloadFeatures();

        Svc.PluginInterface.UiBuilder.Draw -= Ws.Draw;
        Svc.PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
        Ws.RemoveAllWindows();
        MainWindow = null;
        Ws = null;
        ECommonsMain.Dispose();
        PunishLibMain.Dispose();
        FeatureProviders.Clear();
        Common.Shutdown();
        PandoraIPC.Dispose();
        Events.Disable();
        AFKTimer.Dispose();
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

