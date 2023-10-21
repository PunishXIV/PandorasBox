using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Memory;
using ECommons;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PandorasBox.Features.Commands
{
    public unsafe class QuickPlateLink : CommandFeature
    {
        public override string Name => "Glamour Plate Link";
        public override string Command { get; set; } = "/pglamlink";
        public override string[] Alias => new string[] { "/pgl" };

        public override List<string> Parameters => new() { "[-r <role>]", "[-j <jobs>]", "[-g <gearset names/nums>]", "[-n plate number]" };
        public override string Description => "For quickly linking multiple gearsets to a glamour plate.";

        public struct Gearset
        {
            public byte ID { get; set; }
            public int Slot { get; set; }
            public string Name { get; set; }
            public uint ClassJob { get; set; }
            public byte GlamPlate { get; set; }
        }

        private readonly List<Gearset> gearsets = new();
        private readonly List<string> roles = new() { "tanks", "healers", "dps", "ranged", "casters", "magical ranged", "melees", "physical ranged", "doh", "crafters", "doh", "gatherers" };
        private List<ClassJob> jobsList = new();

        protected override void OnCommand(List<string> args)
        {
            gearsets.Clear();

            var sortedArgs = new Dictionary<string, List<string>>();

            for(var i = 0; i < args.Count; i++)
            {
                var currentArg = args[i];

                if (currentArg.StartsWith("-"))
                {
                    var flag = currentArg;
                    var values = new List<string>();

                    if (i + 1 < args.Count && !args[i + 1].StartsWith("-"))
                    {
                        for (var j = i + 1; j < args.Count && !args[j].StartsWith("-"); j++)
                        {
                            values.Add(args[j]);
                            i = j;
                        }
                    }

                    sortedArgs[flag] = values;
                }
            }

            if (sortedArgs.ContainsKey("-h"))
            {
                PrintModuleMessage("Usage: /pgl -j <jobs> <plate number>\n/pgl -r <role> <plate number>");
                return;
            }

            if (sortedArgs.TryGetValue("-j", out var plateValues) && plateValues.Count == 0)
            {
                PrintModuleMessage("Invalid glamour plate number");
                return;
            }

            if (sortedArgs.TryGetValue("-j", out var jobValues) && jobValues.Count == 0)
            {
                PrintModuleMessage("Invalid job names");
                return;
            }

            if (sortedArgs.TryGetValue("-r", out var roleValues) && roleValues.Count == 0)
            {
                PrintModuleMessage($"Invalid role names\nValid role names: {string.Join(", ", roles)}");
                return;
            }

            var plate = byte.Parse(sortedArgs["-n"][0]);
            foreach (var kvp in sortedArgs)
            {
                var flag = kvp.Key;
                var values = kvp.Value;
                switch (flag)
                {
                    case "-r":
                        if (IsRoleMatch(string.Join(" ", values), out var role))
                        {
                            switch (role)
                            {
                                case "tanks":
                                    jobsList = Svc.Data.GetExcelSheet<ClassJob>().Where(x => x.Role.EqualsAny<byte>(1)).ToList();
                                    break;
                                case "healers":
                                    jobsList = Svc.Data.GetExcelSheet<ClassJob>().Where(x => x.Role.EqualsAny<byte>(4)).ToList();
                                    break;
                                case "dps":
                                    jobsList = Svc.Data.GetExcelSheet<ClassJob>().Where(x => x.Role.EqualsAny<byte>(2, 3)).ToList();
                                    break;
                                case "melees":
                                    jobsList = Svc.Data.GetExcelSheet<ClassJob>().Where(x => x.Role.EqualsAny<byte>(2)).ToList();
                                    break;
                                case "ranged":
                                    jobsList = Svc.Data.GetExcelSheet<ClassJob>().Where(x => (x.UIPriority / 10).EqualsAny<int>(3, 4)).ToList();
                                    break;
                                case "magical ranged":
                                case "casters":
                                    jobsList = Svc.Data.GetExcelSheet<ClassJob>().Where(x => (x.UIPriority / 10).Equals(4)).ToList();
                                    break;
                                case "physical ranged":
                                    jobsList = Svc.Data.GetExcelSheet<ClassJob>().Where(x => (x.UIPriority / 10).Equals(3)).ToList();
                                    break;
                                case "crafters":
                                case "doh":
                                    jobsList = Svc.Data.GetExcelSheet<ClassJob>().Where(x => (x.UIPriority / 10).Equals(10)).ToList();
                                    break;
                                case "gatherers":
                                case "dol":
                                    jobsList = Svc.Data.GetExcelSheet<ClassJob>().Where(x => (x.UIPriority / 10).Equals(20)).ToList();
                                    break;
                            }
                            ParseGearset(jobsList.Select(job => job.Name.RawString).ToList(), plate);
                            LinkPlateToGearset(plate);
                        }
                        else
                        {
                            PrintModuleMessage($"Invalid role name passed as an argument.\nRoles: {string.Join(", ", roles)}");
                        }
                        break;
                    case "-g":
                    case "-j":
                        ParseGearset(values, plate);
                        if (gearsets == null || gearsets.Count == 0)
                            PrintModuleMessage($"Unable to match {string.Join(" ", values)} to any gearsets");
                        LinkPlateToGearset(plate);
                        break;
                }
            }             
        }

        private void ParseGearset(List<string> args, byte plate)
        {
            var gearsetModule = RaptureGearsetModule.Instance();

            foreach (var arg in args)
            {
                for (var i = 0; i < 100; i++)
                {
                    var gs = gearsetModule->GetGearset(i);
                    if (gs == null || !gs->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.Exists) || gs->ID != i)
                        continue;

                    var name = MemoryHelper.ReadString(new IntPtr(gs->Name), 47);
                    var jobAbbrMatch = Svc.Data.GetExcelSheet<ClassJob>().Where(x => x.Abbreviation.RawString.Equals(arg, StringComparison.CurrentCultureIgnoreCase)).ToList();
                    
                    if (arg.Equals(name, StringComparison.CurrentCultureIgnoreCase)
                        || (jobAbbrMatch.Count > 0 && jobAbbrMatch[0].RowId.Equals(gs->ClassJob))
                        || arg.Equals(gs->ID.ToString(), StringComparison.CurrentCultureIgnoreCase)
                        || arg.Equals(gs->ClassJob.ToString(), StringComparison.CurrentCultureIgnoreCase))
                    {
                        gearsets.Add(new Gearset
                        {
                            ID = gs->ID,
                            Slot = i + 1,
                            ClassJob = gs->ClassJob,
                            Name = name,
                            GlamPlate = plate,
                        });
                    }
                }
            }
        }

        private void LinkPlateToGearset(byte plate)
        {
            var gearsetModule = RaptureGearsetModule.Instance();

            foreach (var gs in gearsets)
            {
                gearsetModule->LinkGlamourPlate(gs.ID, gs.GlamPlate);
                var msg = new SeStringBuilder()
                    .AddText("Changed gearset ")
                    .AddUiForeground($"{gs.Name} ", 576)
                    .AddText("to use plate ")
                    .AddUiForeground($"{gs.GlamPlate}", 576)
                    .Build();
                PrintModuleMessage(msg);
            }
        }

        private bool IsRoleMatch(string input, out string matchedRole)
        {
            var inputSplit = input.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            matchedRole = roles.FirstOrDefault(role =>
            {
                var rolesSplit = role.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                return inputSplit.All(x => rolesSplit.Any(roleWord => roleWord.Contains(x)));
            });

            return matchedRole != null;
        }
    }
}
