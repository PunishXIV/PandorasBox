using Dalamud.Game;
using Dalamud.Logging;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace PandorasBox.Features.Targets
{
    public unsafe class AutoOpenChests : Feature
    {
        private uint lastChestId = 0;

        public override string Name => "Automatically Open Chests";

        public override string Description => "Walk up to a chest to automatically open it.";

        public override FeatureType FeatureType => FeatureType.Targeting;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Set delay (seconds)", FloatMin = 0.1f, FloatMax = 10f, EditorSize = 300, EnforcedLimit = true)]
            public float Throttle = 0.1f;

            [FeatureConfigOption("Immediately Close Loot Window After Opening", "", 1)]
            public bool CloseLootWindow = false;

            [FeatureConfigOption("Open Chests in High End Duties", "", 2)]
            public bool OpenInHighEndDuty = false;
        }

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => true;

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        private void RunFeature(IFramework framework)
        { 
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas])
            {
                TaskManager.Abort();
                return;
            }

            var contentFinderInfo = Svc.Data.GetExcelSheet<ContentFinderCondition>().GetRow(GameMain.Instance()->CurrentContentFinderConditionId);
            if (!Config.OpenInHighEndDuty && contentFinderInfo.HighEndDuty)
            {
                TaskManager.Abort();
                return;
            }

            var nearbyNodes = Svc.Objects.Where(x => x.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Treasure && GameObjectHelper.GetTargetDistance(x) <= 2 && GameObjectHelper.GetHeightDifference(x) < 1).ToList();
            if (nearbyNodes.Count == 0)
                return;

            var nearestNode = nearbyNodes.OrderBy(GameObjectHelper.GetTargetDistance).First();
            var baseObj = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)nearestNode.Address;

            if (!baseObj->GetIsTargetable())
                return;

            //Opened it.
            if (lastChestId == nearestNode.ObjectId) return;

            if (!TaskManager.IsBusy)
            {
                TaskManager.DelayNext("Chests", (int)(Config.Throttle * 1000));
                TaskManager.Enqueue(() =>
                {
                    if (GameObjectHelper.GetTargetDistance(nearestNode) > 2) return true;
                    TargetSystem.Instance()->InteractWithObject(baseObj, true);
                    lastChestId = nearestNode.ObjectId;
                    if (Config.CloseLootWindow)
                    {
                        TaskManager.DelayNextImmediate(100);
                        TaskManager.EnqueueImmediate(() => CloseWindow(), 5000, false);
                    }
                    return true;
                }, 10, true);
            }
        }

        private static unsafe bool? CloseWindow()
        {
            if (Svc.GameGui.GetAddonByName("NeedGreed", 1) != IntPtr.Zero)
            {
                var needGreedWindow = (AtkUnitBase*)Svc.GameGui.GetAddonByName("NeedGreed", 1);
                if (needGreedWindow == null) return false;

                if (needGreedWindow->IsVisible)
                {
                    needGreedWindow->Close(true);
                    return true;
                }
            }
            else
            {
                return false;
            }

            return false;
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }
    }
}
