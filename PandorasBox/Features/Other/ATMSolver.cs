using ECommons.Automation;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using PandorasBox.FeaturesSetup;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ECommons.GenericHelpers;

namespace PandorasBox.Features.Other
{
    internal class ATMSolver : Feature
    {
        public override string Name { get; } = "Auto Active Time Maneuver";
        public override string Description { get; } = "Automatically hits a button or mashes for you when the ATM is on screen.";

        public override FeatureType FeatureType => FeatureType.Other;
        private long Throttler { get; set; } = Environment.TickCount64;
        private Random random = new Random();
        public override void Enable()
        {
            Svc.Framework.Update += RunFeature;
            base.Enable();
        }

        private unsafe void RunFeature(IFramework framework)
        {
            if (TryGetAddonByName<AtkUnitBase>("QTE", out var addon) && addon->IsVisible)
            {
                if (Environment.TickCount64 >= Throttler)
                {
                    WindowsKeypress.SendKeypress(System.Windows.Forms.Keys.A); //Mashes to try and resolve the QTE
                    Throttler = Environment.TickCount64 + random.Next(50, 75);
                }
            }
        }

        public override void Disable()
        {
            Svc.Framework.Update -= RunFeature;
            base.Disable();
        }
    }
}
