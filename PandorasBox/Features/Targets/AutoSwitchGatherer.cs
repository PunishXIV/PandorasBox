using Dalamud.Configuration;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Logging;
using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using System.Linq;

namespace PandorasBox.Features.Targets
{
    public unsafe class AutoSwitchGatherer : Feature
    {
        public override string Name => "Switch Gatherers Automatically";

        public override string Description => "Switches to the appropriate gathering job when approaching a gathering spot and you have both Triangulate & Prospect active. Must have a gearset for the job to switch to. (Excluding FSH)";

        public override FeatureType FeatureType => FeatureType.Other;

        private const float slowCheckInterval = 1f;
        private float slowCheckRemaining = 0.0f;

        public override void Enable()
        {
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        private void RunFeature(Dalamud.Game.Framework framework)
        {
            if (Svc.ClientState.LocalPlayer == null) return;
            slowCheckRemaining -= (float)Svc.Framework.UpdateDelta.Milliseconds / 1000;

            if (slowCheckRemaining <= 0.0f)
            {
                slowCheckRemaining = slowCheckInterval;

                if (Svc.ClientState.LocalPlayer.StatusList.Where(x => x.StatusId == 217 || x.StatusId == 225).Count() != 2)
                    return;

                if (P.TaskManager.NumQueuedTasks > 0)
                    return;

                var nearbyNodes = Svc.Objects.Where(x => x.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.GatheringPoint && GameObjectHelper.GetTargetDistance(x) < 5).ToList();
                if (nearbyNodes.Count == 0)
                    return;

                var nearestNode = nearbyNodes.OrderBy(GameObjectHelper.GetTargetDistance).First();
                var baseObj = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)nearestNode.Address;

                if (!baseObj->GetIsTargetable())
                    return;

                var gatheringPoint = Svc.Data.GetExcelSheet<GatheringPoint>().First(x => x.RowId == nearestNode.DataId);
                var job = gatheringPoint.GatheringPointBase.Value.GatheringType.Value.RowId;

                if (job is 0 or 1 && Svc.ClientState.LocalPlayer.ClassJob.Id != 16 && !P.TaskManager.IsBusy)
                {
                    P.TaskManager.Enqueue(() => SwitchJobGearset(16), 1000);
                    return;
                }
                if (job is 2 or 3 && Svc.ClientState.LocalPlayer.ClassJob.Id != 17 && !P.TaskManager.IsBusy)
                {
                    P.TaskManager.Enqueue(() => SwitchJobGearset(17), 1000);
                    return;
                }
            }
        }

        private unsafe bool SwitchJobGearset(uint cjID)
        {
            if (Svc.ClientState.LocalPlayer.ClassJob.Id == cjID) return true;
            var gs = GetGearsetForClassJob(cjID);
            if (gs is null) return true;

            Chat chat = new();
            chat.SendMessage($"/gearset change {gs.Value + 1}");

            return true;
        }

        private unsafe static byte? GetGearsetForClassJob(uint cjId)
        {
            var gearsetModule = RaptureGearsetModule.Instance();
            for (var i = 0; i < 100; i++)
            {
                var gearset = gearsetModule->Gearset[i];
                if (gearset == null) continue;
                if (!gearset->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.Exists)) continue;
                if (gearset->ID != i) continue;
                if (gearset->ClassJob == cjId) return gearset->ID;
            }
            return null;
        }

        public override void Disable()
        {
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }
    }
}
