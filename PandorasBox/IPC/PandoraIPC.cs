using ECommons.Automation.NeoTaskManager;
using ECommons.DalamudServices;
using ECommons.Reflection;
using PandorasBox.Features;
using System.Linq;

namespace PandorasBox.IPC
{
    internal static class PandoraIPC
    {
        private static TaskManager TM = new() { RemainingTimeMS = 1000 * 60 * 60 * 24 };
        internal static void Init()
        {
            Svc.PluginInterface.GetIpcProvider<string, bool?>("PandorasBox.GetFeatureEnabled").RegisterFunc(GetFeatureEnabled);
            Svc.PluginInterface.GetIpcProvider<string, bool, object>("PandorasBox.SetFeatureEnabled").RegisterAction(SetFeatureEnabled);

            Svc.PluginInterface.GetIpcProvider<string, string, bool?>("PandorasBox.GetConfigEnabled").RegisterFunc(GetConfigEnabled);
            Svc.PluginInterface.GetIpcProvider<string, string, bool, object>("PandorasBox.SetConfigEnabled").RegisterAction(SetConfigEnabled);

            Svc.PluginInterface.GetIpcProvider<string, int, object>("PandorasBox.PauseFeature").RegisterAction(PauseFeature);

            Svc.PluginInterface.GetIpcProvider<string, bool?>("PandorasBox.GetFeatureEnabledInternal").RegisterFunc(GetFeatureEnabledInternal);
            Svc.PluginInterface.GetIpcProvider<string, bool, object>("PandorasBox.SetFeatureEnabledInternal").RegisterAction(SetFeatureEnabledInternal);

            Svc.PluginInterface.GetIpcProvider<string, string, bool?>("PandorasBox.GetConfigEnabledInternal").RegisterFunc(GetConfigEnabledInternal);
            Svc.PluginInterface.GetIpcProvider<string, string, bool, object>("PandorasBox.SetConfigEnabledInternal").RegisterAction(SetConfigEnabledInternal);

            Svc.PluginInterface.GetIpcProvider<string, int, object>("PandorasBox.PauseFeatureInternal").RegisterAction(PauseFeatureInternal);
        }

        internal static void Dispose()
        {
            Svc.PluginInterface.GetIpcProvider<string, bool?>("PandorasBox.GetFeatureEnabled").UnregisterFunc();
            Svc.PluginInterface.GetIpcProvider<string, bool, object>("PandorasBox.SetFeatureEnabled").UnregisterAction();

            Svc.PluginInterface.GetIpcProvider<string, string, bool?>("PandorasBox.GetConfigEnabled").UnregisterFunc();
            Svc.PluginInterface.GetIpcProvider<string, string, bool, object>("PandorasBox.SetConfigEnabled").UnregisterAction();

            Svc.PluginInterface.GetIpcProvider<string, int, object>("PandorasBox.PauseFeature").UnregisterAction();

            Svc.PluginInterface.GetIpcProvider<string, bool?>("PandorasBox.GetFeatureEnabledInternal").UnregisterFunc();
            Svc.PluginInterface.GetIpcProvider<string, bool, object>("PandorasBox.SetFeatureEnabledInternal").UnregisterAction();

            Svc.PluginInterface.GetIpcProvider<string, string, bool?>("PandorasBox.GetConfigEnabledInternal").UnregisterFunc();
            Svc.PluginInterface.GetIpcProvider<string, string, bool, object>("PandorasBox.SetConfigEnabledInternal").UnregisterAction();

            Svc.PluginInterface.GetIpcProvider<string, int, object>("PandorasBox.PauseFeatureInternal").UnregisterAction();
        }

        private static void SetConfigEnabled(string featureName, string configPropName, bool state)
        {
            foreach (var feature in P.Features)
            {
                if (feature.Name == featureName)
                {
                    var config = feature.GetType().GetProperties().FirstOrDefault(x => x.PropertyType.IsSubclassOf(typeof(FeatureConfig)))?.GetValue(feature);

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
                    var config = feature.GetType().GetProperties().FirstOrDefault(x => x.PropertyType.IsSubclassOf(typeof(FeatureConfig)))?.GetValue(feature);

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

        private static void PauseFeature(string featureName, int pauseMS)
        {
            if (GetFeatureEnabled(featureName) == null) return;
            if (GetFeatureEnabled(featureName)!.Value)
            {
                SetFeatureEnabled(featureName, false);
                TM.EnqueueDelay(pauseMS);
                TM.Enqueue(() => SetFeatureEnabled(featureName, true), $"Resuming{featureName}");
            }
        }

        private static void SetConfigEnabledInternal(string internalName, string configPropName, bool state)
        {
            foreach (var feature in P.Features)
            {
                if (feature.GetType().Name == internalName)
                {
                    var config = feature.GetType().GetProperties().FirstOrDefault(x => x.PropertyType.IsSubclassOf(typeof(FeatureConfig)))?.GetValue(feature);

                    if (config == null) return;

                    var prop = config.GetFoP(configPropName);

                    if (prop == null) return;

                    config.SetFoP(configPropName, state);
                }
            }
        }

        private static bool? GetConfigEnabledInternal(string internalName, string configPropName)
        {
            foreach (var feature in P.Features)
            {
                if (feature.GetType().Name == internalName)
                {
                    var config = feature.GetType().GetProperties().FirstOrDefault(x => x.PropertyType.IsSubclassOf(typeof(FeatureConfig)))?.GetValue(feature);

                    if (config == null) return null;

                    var prop = config.GetFoP(configPropName);

                    if (prop == null) return null;

                    return (bool?)prop;
                }
            }

            return null;
        }

        //OK AND THIS ONE AS WELL
        private static void SetFeatureEnabledInternal(string internalName, bool state)
        {
            foreach (var feature in P.Features)
            {
                if (feature.GetType().Name == internalName)
                {
                    if (state)
                        feature.Enable();
                    else
                        feature.Disable();
                }
            }
        }

        private static bool? GetFeatureEnabledInternal(string internalName)
        {
            foreach (var feature in P.Features)
            {
                if (feature.GetType().Name == internalName)
                {
                    return feature.Enabled;
                }
            }

            return null;
        }

        private static void PauseFeatureInternal(string internalName, int pauseMS)
        {
            if (GetFeatureEnabledInternal(internalName) == null) return;
            if (GetFeatureEnabledInternal(internalName)!.Value)
            {
                SetFeatureEnabledInternal(internalName, false);
                TM.EnqueueDelay(pauseMS);
                TM.Enqueue(() => SetFeatureEnabledInternal(internalName, true), $"Resuming{internalName}");
            }
        }


    }
}
