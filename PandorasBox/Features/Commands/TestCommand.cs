using ECommons.DalamudServices;
using PandorasBox.FeaturesSetup;
using System.Collections.Generic;
using System.Linq;

namespace PandorasBox.Features.Commands
{
    public unsafe class TestCommand : CommandFeature
    {
        public override string Name => "Test Command";
        public override string Command { get; set; } = "/ptest";
        public override string[] Alias => new string[] { "/pt" };

        public override List<string> Parameters => new() { "test", "test2", "test3" };
        public override string Description => "This is a test command.";

        public override FeatureType FeatureType => FeatureType.Disabled;
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
