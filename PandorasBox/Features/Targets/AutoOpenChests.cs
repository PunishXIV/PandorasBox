using Dalamud.Game;
using Dalamud.Logging;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Component.GUI;
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
        public override string Name => "Automatically Open Chests";

        public override string Description => "Walk up to a chest to automatically open it.";

        public override FeatureType FeatureType => FeatureType.Targeting;

        public class Configs : FeatureConfig
        {
            [FeatureConfigOption("Set delay (seconds)", FloatMin = 0.1f, FloatMax = 10f, EditorSize = 300, EnforcedLimit = true)]
            public float Throttle = 0.1f;

            [FeatureConfigOption("Auto Close loot window")]
            public bool CloseLootWindow = false;
        }

        public Configs Config { get; private set; }

        public override bool UseAutoConfig => true;

        public override void Enable()
        {
            Config = LoadConfig<Configs>() ?? new Configs();
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        private void RunFeature(Framework framework)
        {
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas])
            {
                TaskManager.Abort();
                return;
            }
            var nearbyNodes = Svc.Objects.Where(x => x.ObjectKind == Dalamud.Game.ClientState.Objects.Enums.ObjectKind.Treasure && GameObjectHelper.GetTargetDistance(x) <= 2).ToList();
            if (nearbyNodes.Count == 0)
                return;

            var nearestNode = nearbyNodes.OrderBy(GameObjectHelper.GetTargetDistance).First();
            var baseObj = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)nearestNode.Address;

            if (!baseObj->GetIsTargetable())
                return;

            if (!TaskManager.IsBusy)
            {
                TaskManager.DelayNext("Chests", (int)(Config.Throttle * 1000));
                TaskManager.Enqueue(() =>
                {
                    if (GameObjectHelper.GetTargetDistance(nearestNode) > 2) return true;
                    TargetSystem.Instance()->InteractWithObject(baseObj, true);
                    if (Config.CloseLootWindow)
                    {
                        TaskManager.Enqueue(CloseWindow, 200, true);
                    }
                    return true;
                }, 10, true);
            }
        }

        private unsafe static bool? CloseWindow()
        {
            var needGreedWindow = Svc.GameGui.GetAddonByName("NeedGreed", 1);
            if (needGreedWindow == IntPtr.Zero) return false;

            var notification = (AtkUnitBase*)Svc.GameGui.GetAddonByName("_Notification", 1);
            if (notification == null) return false;

            var atkValues = (AtkValue*)Marshal.AllocHGlobal(2 * sizeof(AtkValue));
            atkValues[0].Type = atkValues[1].Type = ValueType.Int;
            atkValues[0].Int = 0;
            atkValues[1].Int = 2;
            try
            {
                notification->FireCallback(2, atkValues);
                return true;
            }
            catch (Exception ex)
            {
                PluginLog.Warning(ex, "Failed to close the window!");
                return false;
            }
            finally
            {
                Marshal.FreeHGlobal(new IntPtr(atkValues));
            }
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }
    }
}
