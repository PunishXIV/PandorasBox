using ECommons.Automation;
using ECommons.DalamudServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandorasBox.Features.Commands
{
    public unsafe class TestCommand : CommandFeature
    {
        public override string Name => "Test Command";
        public override string Command { get; set; } = "/pan-test";
        public override string[] Alias => new string[] { "/pan-t" };

        public override List<string> Parameters => new List<string>() { "test", "test2", "test3" };
        public override string Description => "This is a test command.";
        protected override void OnCommand(List<string> args)
        {
            foreach (var p in Parameters)
            {
                if (args.Any(x => x == p))
                {
                    Svc.Chat.Print($"Test command executed with argument {p}.");
                }
            }
        }
    }
}
