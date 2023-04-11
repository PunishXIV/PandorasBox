using Dalamud.Logging;
using ECommons;
using ECommons.DalamudServices;
using ECommons.Loader;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.GUI;
using PandorasBox.FeaturesSetup;
using System;
using System.Collections.Generic;
using static ECommons.GenericHelpers;
using static System.Collections.Specialized.BitVector32;

namespace PandorasBox.Features.UI
{
    public unsafe class AutoSelectTurnin : Feature
    {
        public override string Name => "Auto-select Turn-ins";

        public override string Description => "Whenever you have to select an item to turn in, it will automatically fill in the interface.";

        public override FeatureType FeatureType => FeatureType.UI;

        private const float slowCheckInterval = 0.1f;
        private float slowCheckRemaining = 0.0f;

        List<int> SlotsFilled = new();
        public override void Enable()
        {
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        private void RunFeature(Dalamud.Game.Framework framework)
        {
            slowCheckRemaining -= (float)Svc.Framework.UpdateDelta.Milliseconds / 1000;

            if (slowCheckRemaining <= 0.0f)
            {
                slowCheckRemaining = slowCheckInterval;
                if (TryGetAddonByName<AddonRequest>("Request", out var addon))
                {
                    for (int i = 1; i <= addon->EntryCount; i++)
                    {
                        if (SlotsFilled.Contains(i)) return;
                        int val = i;
                        P.TaskManager.Enqueue(() => TryClickItem(addon, val));
                    }
                }
                else
                {
                    SlotsFilled.Clear();
                }
            }
        }

        private bool? TryClickItem(AddonRequest* addon, int i)
        {
            if (SlotsFilled.Contains(i)) return true;

            var contextMenu = (AtkUnitBase*)Svc.GameGui.GetAddonByName("ContextIconMenu", 1);
            if (contextMenu == null) return null;

            if (!contextMenu->IsVisible)
            {
                int slot = i - 1;
                int unk = (44 * i) + (i - 1);

                Callback(&addon->AtkUnitBase, 2, slot, 0, 0);
            }
            else
            {
                Callback(contextMenu, 0, 0, 1021003, 0, 0);
                SlotsFilled.Add(i);
            }

            return true;
        }

        public override void Disable()
        {
            Svc.Framework.Update -= RunFeature;
            SlotsFilled.Clear();
            base.Disable();
        }
    }
}
