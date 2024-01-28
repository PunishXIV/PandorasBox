using ECommons.DalamudServices;
using ECommons.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandorasBox.IPC
{
    public static class TeleporterIPC
    {
        public static bool IsEnabled()
        {
            if (DalamudReflector.TryGetDalamudPlugin("Teleporter", out var plugin, true, true))
            {
                return true;
            }
            return false;
        }
    }
}
