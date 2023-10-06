using PandorasBox.Features;
using PandorasBox.Features.UI;
using System;
using System.Linq;
using System.Reflection;

namespace PandorasBox.Helpers
{
    public static class FeatureHelper
    {
        private static bool IsEnabled(BaseFeature feature)
        {
            return Config.EnabledFeatures.Contains(feature.GetType().Name);
        }

        public static bool IsEnabled<T>() where T : BaseFeature
        {
            var assembly = Assembly.GetExecutingAssembly();
            var t = assembly.GetTypes().Where(x => x == typeof(T)).First();
            var f = (T)Activator.CreateInstance(t);

            return IsEnabled(f);

        }

        internal static bool IsBusy => P.TaskManager.IsBusy || WorkshopTurnin.active || AutoSelectTurnin.active;
    }
}
