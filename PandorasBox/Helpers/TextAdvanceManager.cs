using ECommons.DalamudServices;
using ECommons.Logging;
using PandorasBox.Helpers;
using System.Collections.Generic;

namespace AutoRetainer.Modules
{
    internal static class TextAdvanceManager
    {
        private static bool WasChanged = false;

        private static bool IsBusy => FeatureHelper.IsBusy;
        internal static void Tick()
        {
            if (WasChanged)
            {
                if (!IsBusy)
                {
                    WasChanged = false;
                    UnlockTA();
                    PluginLog.Debug($"TextAdvance unlocked");
                }
            }
            if (IsBusy)
            {
                WasChanged = true;
                LockTA();
                PluginLog.Debug($"TextAdvance locked");
            }
        }
        internal static void LockTA()
        {
            if (Svc.PluginInterface.TryGetData<HashSet<string>>("TextAdvance.StopRequests", out var data))
            {
                data.Add(P.Name);
            }
        }

        internal static void UnlockTA()
        {
            if (Svc.PluginInterface.TryGetData<HashSet<string>>("TextAdvance.StopRequests", out var data))
            {
                data.Remove(P.Name);
            }
        }
    }
}
