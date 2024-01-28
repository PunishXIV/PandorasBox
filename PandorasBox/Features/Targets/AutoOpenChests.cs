using Dalamud.Game.ClientState.Objects.Enums;
using ECommons.DalamudServices;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel.GeneratedSheets;
using PandorasBox.FeaturesSetup;
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
        private static uint LastChestId = 0;

        private void RunFeature(IFramework framework)
        { 
            if (Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas])
                return;

            var contentFinderInfo = Svc.Data.GetExcelSheet<ContentFinderCondition>()!.GetRow(GameMain.Instance()->CurrentContentFinderConditionId);
            if (!Config.OpenInHighEndDuty && contentFinderInfo is not null && contentFinderInfo.HighEndDuty)
                return;

            var player = Player.Object;
            var treasure = Svc.Objects.FirstOrDefault(o =>
            {
                if (o == null) return false;
                var dis = Vector3.Distance(player.Position, o.Position) - player.HitboxRadius - o.HitboxRadius;
                if (dis > 0.5f) return false;

                var obj = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)(void*)o.Address;
                if (!obj->GetIsTargetable()) return false;
                if ((ObjectKind)obj->ObjectKind != ObjectKind.Treasure) return false;

                // Opened
                foreach (var item in Loot.Instance()->ItemArraySpan)
                    if (item.ChestObjectId == o.ObjectId) return false;

                return true;
            });

            if (treasure == null) return;
            if (DateTime.Now < NextOpenTime) return;
            if (treasure.ObjectId == LastChestId && DateTime.Now - NextOpenTime < TimeSpan.FromSeconds(10)) return;

            NextOpenTime = DateTime.Now.AddSeconds(new Random().NextDouble() + 0.2);
            LastChestId = treasure.ObjectId;

            try
            {
                Svc.Targets.Target = treasure;
                TargetSystem.Instance()->InteractWithObject((FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)(void*)treasure.Address);
            }
            catch (Exception ex)
            {
                Svc.Log.Error(ex, "Failed to open the chest!");
            }

            if (Config.CloseLootWindow)
            {
                CloseWindowTime = DateTime.Now.AddSeconds(0.5);
                CloseWindow();
            }
        }

        private static DateTime CloseWindowTime = DateTime.Now;
        private static unsafe void CloseWindow()
        {
            if (CloseWindowTime < DateTime.Now) return;
            if (Svc.GameGui.GetAddonByName("NeedGreed", 1) != IntPtr.Zero)
            {
                var needGreedWindow = (AtkUnitBase*)Svc.GameGui.GetAddonByName("NeedGreed", 1);
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
