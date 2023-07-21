using Dalamud.Logging;
using ECommons.Automation;
using ECommons.DalamudServices;
using PandorasBox.FeaturesSetup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandorasBox.Features.Commands
{
    public unsafe class QuickPlateLink : CommandFeature
    {
        public override string Name => "Glamour Plate Link";
        public override string Command { get; set; } = "/pglamlink";
        public override string[] Alias => new string[] { "/pgl" };

        public override List<string> Parameters => new() { "[jobs/roles/gearsets]", "[num]" };
        public override string Description => "For quickly linking multiple gearsets to a glamour plate.";

        public override FeatureType FeatureType => FeatureType.Commands;
        protected override void OnCommand(List<string> args)
        {
            if (!int.TryParse(args[^1], out var noInts))
            {
                PrintModuleMessage("test");
                return; }
                
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
