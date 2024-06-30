using ECommons.Reflection;

namespace PandorasBox.IPC
{
    public static class TeleporterIPC
    {
        public static bool IsEnabled()
        {
            if (DalamudReflector.TryGetDalamudPlugin("TeleporterPlugin", out var plugin, true, true))
            {
                return true;
            }
            return false;
        }
    }
}
