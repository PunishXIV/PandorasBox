using ECommons.Automation;
using ECommons.DalamudServices;
using ECommons.Logging;
using FFXIVClientStructs.FFXIV.Component.GUI;
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

        public override List<string> Parameters => new() { "addonName", "updateStateBool", "atkValues" };
        public override string Description => "Fires arbitrary callbacks to any addon of your choosing. Play with responsibly.";

        protected override void OnCommandInternal(string _, string args)
        {
            OnCommand(args.Split(' ').ToList());
        }

        protected override void OnCommand(List<string> args)
        {
            if (!TryGetAddonByName<AtkUnitBase>(args[0], out var addonArg))
            {
                Svc.Log.Debug($"Invalid addon {args[0]}. Please follow \"{Command} <addon> <bool> <atkValues>\"");
                return;
            }
            if (!bool.TryParse(args[1], out var boolArg))
            {
                Svc.Log.Debug($"Invalid bool. Please follow \"{Command} <addon> <bool> <atkValues>\"");
                return;
            }

            var valueArgs = ParseValueArguments(args, 2);
            Callback.Fire(addonArg, boolArg, valueArgs.ToArray());
        }

        private static List<object> ParseValueArguments(List<string> args, int startIndex)
        {
            var valueArgs = new List<object>();

            var current = "";
            var inQuotes = false;

            for (var i = startIndex; i < args.Count; i++)
            {
                if (!inQuotes)
                {
                    if (args[i].StartsWith("\""))
                    {
                        inQuotes = true;
                        current = args[i].TrimStart('"');
                    }
                    else
                    {
                        if (int.TryParse(args[i], out var iValue)) valueArgs.Add(iValue);
                        else if (uint.TryParse(args[i].TrimEnd('U', 'u'), out var uValue)) valueArgs.Add(uValue);
                        else if (bool.TryParse(args[i], out var bValue)) valueArgs.Add(bValue);
                        else valueArgs.Add(args[i]);
                    }
                }
                else
                {
                    if (args[i].EndsWith("\""))
                    {
                        inQuotes = false;
                        current += " " + args[i].TrimEnd('"');
                        valueArgs.Add(current);
                        current = "";
                    }
                    else
                    {
                        current += " " + args[i];
                    }
                }
            }

            if (!string.IsNullOrEmpty(current))
            {
                Svc.Log.Debug("Error: Unclosed quotes.");
            }

            return valueArgs;
        }
    }
}
