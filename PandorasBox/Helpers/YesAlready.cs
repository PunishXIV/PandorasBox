using ECommons.DalamudServices;
using ECommons.Reflection;

namespace PandorasBox.Helpers;

internal static class YesAlready
{
    internal static bool Reenable = false;
    internal static void DisableIfNeeded()
    {
        if (DalamudReflector.TryGetDalamudPlugin("Yes Already", out var pl, false, true))
        {
            Svc.Log.Information("Disabling Yes Already");
            pl.GetStaticFoP("YesAlready.Service", "Configuration").SetFoP("Enabled", false);
            Reenable = true;
        }
    }

    internal static void EnableIfNeeded()
    {
        if (Reenable && DalamudReflector.TryGetDalamudPlugin("Yes Already", out var pl, false, true))
        {
            Svc.Log.Information("Enabling Yes Already");
            pl.GetStaticFoP("YesAlready.Service", "Configuration").SetFoP("Enabled", true);
            Reenable = false;
        }
    }

    internal static bool IsEnabled()
    {
        if (DalamudReflector.TryGetDalamudPlugin("Yes Already", out var pl, false, true))
        {
            return pl.GetStaticFoP("YesAlready.Service", "Configuration").GetFoP<bool>("Enabled");
        }
        return false;
    }

    internal static bool? WaitForYesAlreadyDisabledTask()
    {
        return !IsEnabled();
    }
}
