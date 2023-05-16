using Dalamud.Game.Command;
using ECommons.DalamudServices;
using ECommons.Logging;
using PandorasBox.FeaturesSetup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PandorasBox.Features;

public abstract class CommandFeature : Feature
{
    public abstract string Command { get; set; }
    public virtual string[] Alias => Array.Empty<string>();
    public virtual string HelpMessage => $"[{P.Name} {Name}]";
    public virtual bool ShowInHelp => false;
    public virtual List<string> Parameters => new List<string>();
    public override FeatureType FeatureType => FeatureType.Commands;

    protected abstract void OnCommand(List<string> args);

    private void OnCommandInternal(string _, string args)
    {
        args = args.ToLower();
        OnCommand(args.Split(' ').ToList());
    }

    private List<string> registeredCommands = new();

    public override void Enable()
    {
        var c = Command.StartsWith("/pan") ? Command : $"/pan {Command}";
        if (Svc.Commands.Commands.ContainsKey(c))
        {
            PluginLog.Error($"Command '{c}' is already registered.");
        }
        else
        {
            Svc.Commands.AddHandler(c, new CommandInfo(OnCommandInternal)
            {
                HelpMessage = HelpMessage,
                ShowInHelp = ShowInHelp
            });

            registeredCommands.Add(c);
        }

        foreach (var a in Alias)
        {
            var alias = a.StartsWith("/pan") ? a : $"/pan {a}";
            if (!Svc.Commands.Commands.ContainsKey(alias))
            {
                Svc.Commands.AddHandler(alias, new CommandInfo(OnCommandInternal)
                {
                    HelpMessage = HelpMessage,
                    ShowInHelp = false
                });
                registeredCommands.Add(alias);
            }
        }
    }

    public override void Disable()
    {
        foreach (var c in registeredCommands)
        {
            Svc.Commands.RemoveHandler(c);
        }
        registeredCommands.Clear();
        base.Disable();
    }

    public List<string> GetArgumentList(string args) => Regex.Matches(args, @"[\""].+?[\""]|[^ ]+")
    .Select(m =>
    {
        if (m.Value.StartsWith('"') && m.Value.EndsWith('"')) { return m.Value.Substring(1, m.Value.Length - 2); }
        return m.Value;
    })
    .ToList();
}
