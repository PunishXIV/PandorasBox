using ECommons.DalamudServices;
using ECommons.Reflection;
using PandorasBox.Features;
using System.Linq;

namespace PandorasBox.IPC
{
    internal static class PandoraIPC
    {
        internal static void Init()
        {
            Svc.PluginInterface.GetIpcProvider<string, bool?>("PandorasBox.GetFeatureEnabled").RegisterFunc(GetFeatureEnabled);
            Svc.PluginInterface.GetIpcProvider<string, bool, object>("PandorasBox.SetFeatureEnabled").RegisterAction(SetFeatureEnabled);

            Svc.PluginInterface.GetIpcProvider<string, string, bool?>("PandorasBox.GetConfigEnabled").RegisterFunc(GetConfigEnabled);
            Svc.PluginInterface.GetIpcProvider<string, string, bool, object>("PandorasBox.SetConfigEnabled").RegisterAction(SetConfigEnabled);
        }

        internal static void Dispose()
        {
            Svc.PluginInterface.GetIpcProvider<string, bool?>("PandorasBox.GetFeatureEnabled").UnregisterFunc();
            Svc.PluginInterface.GetIpcProvider<string, bool, object>("PandorasBox.SetFeatureEnabled").UnregisterAction();

            Svc.PluginInterface.GetIpcProvider<string, string, bool?>("PandorasBox.GetConfigEnabled").UnregisterFunc();
            Svc.PluginInterface.GetIpcProvider<string, string, bool, object>("PandorasBox.SetConfigEnabled").UnregisterAction();
        }

        private static void SetConfigEnabled(string featureName, string configPropName, bool state)
        {
            foreach (var feature in P.Features)
            {
                if (feature.Name == featureName)
                {
                    var config = feature.GetType().GetProperties().FirstOrDefault(x => x.PropertyType.IsSubclassOf(typeof(FeatureConfig))).GetValue(feature);

                    if (config == null) return;

                    var prop = config.GetFoP(configPropName);

                    if (prop == null) return;

                    config.SetFoP(configPropName, state);
                }
            }
        }

        private static bool? GetConfigEnabled(string featureName, string configPropName)
        {
            foreach (var feature in P.Features)
            {
                if (feature.Name == featureName)
                {
                    var config = feature.GetType().GetProperties().FirstOrDefault(x => x.PropertyType.IsSubclassOf(typeof(FeatureConfig))).GetValue(feature);

                    if (config == null) return null;

                    var prop = config.GetFoP(configPropName);

                    if (prop == null) return null;

                    return (bool?)prop;
                }
            }

            return null;
        }

        private static void SetFeatureEnabled(string featureName, bool state)
        {
            foreach (var feature in P.Features)
            {
                if (feature.Name == featureName)
                {
                    if (state)
                        feature.Enable();
                    else
                        feature.Disable();
                }
            }
        }

        private static bool? GetFeatureEnabled(string featureName)
        {
            foreach (var feature in P.Features)
            {
                if (feature.Name == featureName)
                {
                    return feature.Enabled;
                }
            }

            return null;
        }

    }
}
