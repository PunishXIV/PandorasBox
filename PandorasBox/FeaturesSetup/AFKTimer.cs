using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandorasBox.FeaturesSetup
{
    internal static class AFKTimer
    {
        public static readonly Stopwatch Stopwatch = new Stopwatch();

        public static void Init()
        {
            Svc.Framework.Update += UpdateTimer;
        }

        private unsafe static void UpdateTimer(IFramework framework)
        {
            if (AgentMap.Instance()->IsPlayerMoving == 0 || Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat])
            {
                if (!Stopwatch.IsRunning)
                    Stopwatch.Restart();
            }
            else
            {
                Stopwatch.Reset();
            }
        }

        public static void Dispose()
        {
            Svc.Framework.Update -= UpdateTimer;
        }
    }
}
