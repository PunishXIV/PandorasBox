using PandorasBox.FeaturesSetup;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using System.Linq;
using System.Collections.Generic;
using Lumina.Excel.GeneratedSheets;

namespace PandorasBox.Features.Actions
{
    public unsafe class AutoCordial : Feature
    {
        public override string Name => "Auto-Cordial";

        public override string Description => "Automatically use a cordial when below a given threshold.";

        public override FeatureType FeatureType => FeatureType.Actions;

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => true;

        internal static readonly List<uint> cordialRowIDs = new() { 12669, 6141, 16911 };

        internal static (string Name, uint Id, bool CanBeHQ, ushort NQGP, ushort HQGP)[] rawCordialsData;
        internal static (string Name, uint Id, ushort GP)[] cordials;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("GP Threshold", "", 1, IntMin = 0, IntMax = 1000, EditorSize = 300)]
            public int Threshold = 1000;

            [FeatureConfigOption("Invert Priority (Watered -> Regular -> Hi)", "", 2)]
            public bool InvertPriority = false;

            [FeatureConfigOption("Prevent Overcap", "", 3)]
            public bool PreventOvercap = true;

            [FeatureConfigOption("Use on Fisher", "", 4)]
            public bool UseOnFisher = false;
        }

        private static bool WillOvercap(int gp_recovery)
        {
            return ((int)Svc.ClientState.LocalPlayer.CurrentGp + gp_recovery) > (int)Svc.ClientState.LocalPlayer.MaxGp;
        }

        private void RunFeature(IFramework framework)
        {
            if (Svc.ClientState.LocalPlayer is null) return;
            if (!(Svc.ClientState.LocalPlayer.ClassJob.Id == 16 || Svc.ClientState.LocalPlayer.ClassJob.Id == 17 || Svc.ClientState.LocalPlayer.ClassJob.Id == 18)) return;
            if (Svc.ClientState.LocalPlayer.ClassJob.Id == 18 && !Config.UseOnFisher) return;
            if (Svc.ClientState.LocalPlayer.CurrentGp >= Config.Threshold) return;

            var im = InventoryManager.Instance();
            var inv1 = im->GetInventoryContainer(InventoryType.Inventory1);
            var inv2 = im->GetInventoryContainer(InventoryType.Inventory2);
            var inv3 = im->GetInventoryContainer(InventoryType.Inventory3);
            var inv4 = im->GetInventoryContainer(InventoryType.Inventory4);

            InventoryContainer*[] container = { inv1, inv2, inv3, inv4 };

            var am = ActionManager.Instance();

            foreach (var cordial in Config.InvertPriority ? cordials.Reverse() : cordials)
            {
                foreach (var cont in container)
                {
                    for (var j = 0; j < cont->Size; j++)
                    {
                        if (cont->GetInventorySlot(j)->ItemId == (cordial.Id >= 1000000 ? cordial.Id - 1_000_000 : cordial.Id))
                        {
                            if (am->GetActionStatus(ActionType.Item, cordial.Id) == 0)
                            {
                                if (!Config.PreventOvercap || (Config.PreventOvercap && !WillOvercap(cordial.GP)))
                                {
                                    am->UseAction(ActionType.Item, cordial.Id, extraParam: 65535);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();

            rawCordialsData = Svc.Data.GetExcelSheet<Item>()
                .Where(row => cordialRowIDs.Any(num => num == row.RowId))
                .Select(row => (
                    Name: row.Name.RawString,
                    Id: row.RowId,
                    CanBeHQ: row.CanBeHq,
                    NQGP: row.ItemAction.Value.Data[0],
                    HQGP: row.ItemAction.Value.DataHQ[0]
                )).ToArray();

            cordials = rawCordialsData
                .SelectMany((cordial, index) => cordial.CanBeHQ
                    ? new[]
                    {
                        (Name: cordial.Name, Id: cordial.Id + 1_000_000, GP: cordial.HQGP),
                        (Name: cordial.Name, Id: cordial.Id, GP: cordial.NQGP)
                    }
                    : new[]
                    {
                        (Name: cordial.Name, Id: cordial.Id, GP: cordial.NQGP)
                    }).OrderByDescending(cordial => cordial.GP)
                .ToArray();

            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }
    }
}
