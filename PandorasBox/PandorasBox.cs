using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ECommons;
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

    public static PandorasBox P { get; private set; } = null!;
    public static Configuration Config { get; private set; } = null!;

    public List<FeatureProvider> FeatureProviders = [];
    private FeatureProvider provider;
    public IEnumerable<BaseFeature> Features => FeatureProviders.Where(x => !x.Disposed).SelectMany(x => x.Features).OrderBy(x => x.Name);
    public PandorasBox(IDalamudPluginInterface pluginInterface, IFramework framework)
    {
        P = this;
        Ws = new();
        MainWindow = new();
        provider = new FeatureProvider(Assembly.GetExecutingAssembly());
        _ = framework.RunOnFrameworkThread(() =>
        {
            ECommonsMain.Init(pluginInterface, P, ECommons.Module.All);
            Initialize();
        });
    }

    private void Initialize()
    {
        PunishLibMain.Init(Svc.PluginInterface, "Pandora's Box", new AboutPlugin() { Sponsor = "https://ko-fi.com/taurenkey" });

        Ws.AddWindow(MainWindow);
        Config = Svc.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Config.Initialize(Svc.PluginInterface);

        _ = Svc.Commands.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the Pandora menu.",
            ShowInHelp = true
        });

        Svc.PluginInterface.UiBuilder.Draw += Ws.Draw;
        Svc.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        PandoraIPC.Init();
        Events.Init();
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
        ECommonsMain.Dispose();
        PunishLibMain.Dispose();
        FeatureProviders.Clear();
        PandoraIPC.Dispose();
        Events.Disable();
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

