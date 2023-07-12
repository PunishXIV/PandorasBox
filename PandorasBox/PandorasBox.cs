using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Reflection;
using PandorasBox.Features;
using PandorasBox.UI;
using PunishLib;
using PunishLib.Sponsor;
using System;
using System.Collections;
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
    internal static DalamudPluginInterface pi;
    internal static Configuration Config;

    public List<FeatureProvider> FeatureProviders = new();
    public IEnumerable<BaseFeature> Features => FeatureProviders.Where(x => !x.Disposed).SelectMany(x => x.Features).OrderBy(x => x.Name);
    internal TaskManager TaskManager;

    public PandorasBox(DalamudPluginInterface pluginInterface)
    {
        P = this;
        pi = pluginInterface;
        Initialize();
    }

    private void Initialize()
    {
        ECommonsMain.Init(pi, P, ECommons.Module.All);
        PunishLibMain.Init(pi, P);
        
        //FFXIVClientStructs.Interop.Resolver.GetInstance.SetupSearchSpace(Svc.SigScanner.SearchBase);
        //FFXIVClientStructs.Interop.Resolver.GetInstance.Resolve();

        SponsorManager.SetSponsorInfo("https://ko-fi.com/taurenkey");
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
        var provider = new FeatureProvider(Assembly.GetExecutingAssembly());
        provider.LoadFeatures();
        FeatureProviders.Add(provider);

    }


    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Svc.Commands.RemoveHandler(CommandName);
        foreach (var t in FeatureProviders.Where(t => !t.Disposed))
        {
            t.Dispose();
        }

        Svc.PluginInterface.UiBuilder.Draw -= Ws.Draw;
        Svc.PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
        Ws.RemoveAllWindows();
        MainWindow = null;
        Ws = null;
        ECommonsMain.Dispose();
        PunishLibMain.Dispose();
        FeatureProviders.Clear();
        Common.Shutdown();
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

