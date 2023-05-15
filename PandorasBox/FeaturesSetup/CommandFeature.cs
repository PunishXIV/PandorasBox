using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Game.Command;
using ECommons.DalamudServices;
using ECommons.Logging;

namespace PandorasBox.Features;

public abstract class CommandFeature : Feature
{
    protected abstract string Command { get; }
    protected virtual string[] Alias => Array.Empty<string>();
    protected virtual string HelpMessage => $"[{P.Name} {Name}";
    protected virtual bool ShowInHelp => true;

    protected abstract void OnCommand(string args);

    private void OnCommandInternal(string _, string args) => OnCommand(args);

    private List<string> registeredCommands = new();

    public override void Enable()
    {
        var c = Command.StartsWith("/") ? Command : $"/{Command}";
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
            var alias = a.StartsWith("/") ? a : $"/{a}";
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

        base.Enable();
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

    protected List<string> GetArgumentList(string args) => Regex.Matches(args, @"[\""].+?[\""]|[^ ]+")
            .Select(m =>
            {
                if (m.Value.StartsWith('"') && m.Value.EndsWith('"')) { return m.Value.Substring(1, m.Value.Length - 2); }
                return m.Value;
            })
            .ToList();
}
