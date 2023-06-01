using Dalamud.Interface.Windowing;
using ECommons.DalamudServices;
using ECommons.Logging;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;

namespace PandorasBox.Features.UI
{
    public unsafe class WorkshopHelper : Feature
    {
        public override string Name => "Workshop Helper";

        public override string Description => "Save/Load infinite presets. Set the schedule(s) for you. Can import from Overseas Casuals.";

        public override FeatureType FeatureType => FeatureType.UI;
        internal WorkshopWindow WorkshopWindow { get; set; }
        internal static (uint Key, string Name, ushort CraftingTime)[] Craftables;
        public static Dictionary<int, Schedule> CopiedSchedule;
        public static bool _enabled;

        public class Configs : FeatureConfig
        {
            public int SelectedCycle = 1;
        }

        public Configs Config { get; private set; }

        public class Schedule
        {
            public List<Item> Items { get; }

            public Schedule(List<Item> items)
            {
                Items = items;
            }
        }

        public class Item
        {
            public uint Key { get; set; }
            public string Name { get; set; }
            public ushort CraftingTime { get; set; }
        }

        internal static Dictionary<int, Schedule> ScheduleImport(string input)
        {
            List<int> workshops = ParseWorkshops(input);
            List<Item> items = ParseItems(input);

            Dictionary<int, Schedule> schedules = new Dictionary<int, Schedule>();
            foreach (int workshop in workshops)
            {
                schedules[workshop] = new Schedule(items);
            }

            CopiedSchedule = schedules;
            return schedules;
        }

        public static List<int> ParseWorkshops(string input)
        {
            List<int> workshops = new List<int>();

            string pattern = @"Workshops #(\d+)-?(\d+)? Rec|All Workshops|All";
            Match match = Regex.Match(input, pattern);

            if (match.Success)
            {
                if (match.Groups[1].Success) // Single workshop
                {
                    int workshopNumber = int.Parse(match.Groups[1].Value);
                    workshops.Add(workshopNumber);
                }
                else if (match.Groups[2].Success) // Range of workshops
                {
                    int start = int.Parse(match.Groups[1].Value);
                    int end = int.Parse(match.Groups[2].Value);

                    for (int i = start; i <= end; i++)
                    {
                        workshops.Add(i);
                    }
                }
                else if (match.Value.Equals("All Workshops", StringComparison.OrdinalIgnoreCase) ||
                         match.Value.Equals("All", StringComparison.OrdinalIgnoreCase))
                {
                    // Add all workshops (1, 2, 3, 4) to the list
                    for (int i = 1; i <= 4; i++)
                    {
                        workshops.Add(i);
                    }
                }
            }

            return workshops;
        }

        public static List<Item> ParseItems(string input)
        {
            List<Item> items = new List<Item>();

            string[] itemNames = input.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (string itemName in itemNames)
            {
                var matchedCraftable = Craftables.FirstOrDefault(c => c.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));

                if (matchedCraftable.Key != 0)
                {
                    Item item = new Item
                    {
                        Key = matchedCraftable.Key,
                        Name = matchedCraftable.Name,
                        CraftingTime = matchedCraftable.CraftingTime
                    };

                    items.Add(item);
                }
                else
                {
                    Console.WriteLine("Item not found: {0}", itemName);
                }
            }

            return items;
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Craftables = Svc.Data.GetExcelSheet<MJICraftworksObject>()
                .Where(x => !string.IsNullOrEmpty(x.Item.ToString()) || !string.IsNullOrEmpty(x.Theme.ToString()))
                .Select(x => (x.RowId, x.Item.Value.ToString(), x.CraftingTime))
                .ToArray();
            WorkshopWindow = new();
            P.Ws.AddWindow(WorkshopWindow);
            _enabled = true;
            base.Enable();
        }

        public override void Disable()
        {
            SaveConfig(Config);
            P.Ws.RemoveWindow(WorkshopWindow);
            WorkshopWindow = null;
            _enabled = false;
            base.Disable();
        }
    }
}
