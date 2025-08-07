using Dalamud.Game.ClientState.Objects.Enums;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using ECommons.Throttlers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.Sheets;
using PandorasBox.FeaturesSetup;
using PandorasBox.Helpers;
using System;
using System.Linq;
using System.Numerics;

namespace PandorasBox.Features.Targets
{
    public unsafe class AutoOpenChests : Feature
    {
        public override string Name => "Automatically Open Chests";

        public override string Description => "Walk up to a chest to automatically open it.";

        public override FeatureType FeatureType => FeatureType.Targeting;

        public class Configs : FeatureConfig
        {
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

        private static DateTime NextOpenTime = DateTime.Now;
        private static ulong LastChestId = 0;

        private void RunFeature(IFramework framework)
        {
            CloseWindow();

            if (!EzThrottler.Throttle("ChestThrottle", 200))
                return;

            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas])
                return;

            if (!Config.OpenInHighEndDuty && Svc.Data.GetExcelSheet<ContentFinderCondition>().FindFirst(x => x.RowId == GameMain.Instance()->CurrentContentFinderConditionId, out var contentFinderInfo) && contentFinderInfo.HighEndDuty)
                return;

            var player = Player.Object;
            if (player == null) return; 
            var treasure = Svc.Objects.FirstOrDefault(o =>
            {
                if (o == null) return false;
                var dis = Vector3.Distance(player.Position, o.Position);
                if (dis > 2f) return false;

                var obj = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)(void*)o.Address;
                if (!obj->GetIsTargetable()) return false;
                if ((ObjectKind)obj->ObjectKind != ObjectKind.Treasure) return false;

                foreach (var item in Loot.Instance()->Items)
                    if (item.ChestObjectId == o.GameObjectId) return false;

                var tr = (FFXIVClientStructs.FFXIV.Client.Game.Object.Treasure*)obj;
                if (tr->Flags.HasFlag(FFXIVClientStructs.FFXIV.Client.Game.Object.Treasure.TreasureFlags.Opened) ||
                    tr->Flags.HasFlag(FFXIVClientStructs.FFXIV.Client.Game.Object.Treasure.TreasureFlags.FadedOut)) return false;

                return true;
            });

            if (treasure == null) return;
            try
            {
                Svc.Targets.Target = treasure;
                TargetSystem.Instance()->InteractWithObject((FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)(void*)treasure.Address);
                if (Config.CloseLootWindow)
                {
                    CloseWindowTime = DateTime.Now.AddSeconds(0.5);
                }
            }
            catch (Exception ex)
            {
                Svc.Log.Error(ex, "Failed to open the chest!");
            }
        }

        private static DateTime CloseWindowTime = DateTime.Now;
        private static unsafe void CloseWindow()
        {
            if (CloseWindowTime < DateTime.Now) return;
            if (Svc.GameGui.GetAddonByName("NeedGreed", 1) != IntPtr.Zero)
            {
                var needGreedWindow = (AtkUnitBase*)Svc.GameGui.GetAddonByName("NeedGreed", 1).Address;
                if (needGreedWindow == null) return;

                if (needGreedWindow->IsVisible)
                {
                    needGreedWindow->Close(true);
                    return;
                }
            }
            else
            {
                return;
            }

            return;
        }

        public override void Disable()
        {
            SaveConfig(Config);
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }
    }
}
