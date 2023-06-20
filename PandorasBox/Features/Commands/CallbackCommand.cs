using ECommons;
using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Linq;
using static ECommons.GenericHelpers;

namespace PandorasBox.Features.Commands
{
    public unsafe class CallbackCommand : CommandFeature
    {
        public override string Name => "Callback";
        public override string Command { get; set; } = "/pcallback";
        public override string[] Alias => new string[] { "/pcall" };

        public override List<string> Parameters => new() { "addon", "bool", "values" };
        public override string Description => "Fires arbitrary callbacks to any addon of your choosing. Play with responsibly.";

        protected override void OnCommandInternal(string _, string args)
        {
            OnCommand(args.Split(' ').ToList());
        }

        protected override void OnCommand(List<string> args)
        {
            if (!TryGetAddonByName<AtkUnitBase>(args[0], out var addonArg))
            {
                PluginLog.Log($"Invalid addon {args[0]}");
                return;
            }
            if (!bool.TryParse(args[1], out var boolArg))
            {
                PluginLog.Log("invalid bool");
                return;
            }
            var valueArgs = new List<object>();
            for (var i = 2; i < args.Count; i++)
            {
                var current = args[i];
                if (int.TryParse(current, out int iValue)) valueArgs.Add(iValue);
                else if (uint.TryParse(current.TrimEnd('U', 'u'), out uint uValue)) valueArgs.Add(uValue);
                else valueArgs.Add(current);
            }
            Callback.Fire(addonArg, boolArg, valueArgs.ToArray());
        }
    }
}
