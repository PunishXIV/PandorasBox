using Dalamud.Logging;
using Dalamud.Memory;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.GeneratedSheets;
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
                PrintModuleMessage("Invalid glamour plate number");
                return;
            }

            if (args.Count < 2)
            {
                PrintModuleMessage("Invalid number of arguments");
                return;
            }

            foreach (var p in Parameters)
            {
                var jobNames = Svc.Data.GetExcelSheet<ClassJob>().First(x => x.Name.RawString.ToLower() == p.ToLower());
                var jobAbbrs = Svc.Data.GetExcelSheet<ClassJob>().First(x => x.Abbreviation.RawString.ToLower() == p.ToLower());

                var gearsetModule = RaptureGearsetModule.Instance();
                var isGearsetName = false;
                for (var i = 0; i < 100; i++)
                {
                    var gs = gearsetModule->Gearset[i];
                    if (gs == null || !gs->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.Exists) || gs->ID != i)
                        continue;

                    var name = MemoryHelper.ReadString(new IntPtr(gs->Name), 47);
                    isGearsetName = p.ToLower() == name.ToLower() ? true : false;
                }

                if (jobNames != null || jobAbbrs != null || isGearsetName)
                    return;

                if (args.Any(x => x == p))
                {
                    Svc.Chat.Print($"Test command executed with argument {p}.");
                }
            }
        }
    }
}
