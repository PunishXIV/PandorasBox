using ECommons.DalamudServices;
using PandorasBox.FeaturesSetup;
using System.Collections.Generic;
using System.Linq;

namespace PandorasBox.Features.Commands
{
    public unsafe class TestCommand : CommandFeature
    {
        public override string Name => "Test Command";
        public override string Command { get; set; } = "/pan-test";
        public override string[] Alias => new string[] { "/pan-t" };

        public override List<string> Parameters => new() { "test", "test2", "test3" };
        public override string Description => "This is a test command.";

        public override FeatureType FeatureType => FeatureType.Commands;

        public override bool Disabled => true;
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
