using ECommons.DalamudServices;
using ECommons.GameHelpers;
using System.Diagnostics;

namespace PandorasBox.FeaturesSetup
{
    internal static class AFKTimer
    {
        public static readonly Stopwatch Stopwatch = new Stopwatch();

        public static void Init()
        {
            Svc.Framework.Update += UpdateTimer;
        }

        private static unsafe void UpdateTimer(IFramework framework)
        {
            if (Player.AvailableThreadSafe)
            {
                if (Player.IsMoving || Svc.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat])
                {
                    if (!Stopwatch.IsRunning)
                        Stopwatch.Restart();
                }
                else
                {
                    Stopwatch.Reset();
                }
            }
        }

        public static void Dispose()
        {
            Svc.Framework.Update -= UpdateTimer;
        }
    }
}
