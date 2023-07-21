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

        public override List<string> Parameters => new() { "jobs", "num" };
        public override string Description => "For quickly linking multiple gearsets to a glamour plate.";

        public override FeatureType FeatureType => FeatureType.Commands;

        public struct Gearset
        {
            public byte ID { get; set; }
            public int Slot { get; set; }
            public string Name { get; set; }
            public uint ClassJob { get; set; }
            public byte GlamPlate { get; set; }
        }

        private List<Gearset> gearsets = new();
        private List<string> roles = new() { "tank", "healer", "dps", "caster", "melee", "physical ranged", "ranged", "magical ranged" };
        protected override void OnCommand(List<string> args)
        {
            gearsets.Clear();

            if (args.Count < 2)
            {
                PrintModuleMessage("Invalid number of arguments");
                return;
            }
            if (!int.TryParse(args[^1], out var noInts))
            {
                PrintModuleMessage("Invalid glamour plate number");
                return;
            }

            var gearsetModule = RaptureGearsetModule.Instance();
            foreach (var arg in args)
            {
                //if (IsRoleMatch(arg, out var role))
                //    switch (role)
                //    {
                //        case "tank":
                //            break;
                //    }
                for (var i = 0; i < 100; i++)
                {
                    var gs = gearsetModule->Gearset[i];
                    if (gs == null || !gs->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.Exists) || gs->ID != i)
                        continue;

                    var name = MemoryHelper.ReadString(new IntPtr(gs->Name), 47);

                    if (arg.Equals(name, StringComparison.CurrentCultureIgnoreCase) || arg.Equals(gs->ID.ToString(), StringComparison.CurrentCultureIgnoreCase))
                        gearsets.Add(new Gearset
                        {
                            ID = gs->ID,
                            Slot = i + 1,
                            ClassJob = gs->ClassJob,
                            Name = name,
                            GlamPlate = byte.Parse(args[^1]),
                        });
                }
            }

            foreach (var gs in gearsets)
            {
                gearsetModule->LinkGlamourPlate(gs.ID, gs.GlamPlate);
                PrintModuleMessage($"Changed gearset {gs.Name} to use plate {gs.GlamPlate}");
            }
        }
        private bool IsRoleMatch(string input, out string matchedRole)
        {
            // Split the input string into individual words
            var inputSplit = input.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Find roles that partially match the input words
            matchedRole = roles.FirstOrDefault(role =>
            {
                var rolesSplit = role.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return inputSplit.All(x => rolesSplit.Any(roleWord => roleWord.Contains(x)));
            });

            return matchedRole != null;
        }
    }
}
