using Dalamud.Logging;
using PandorasBox.Features;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
    }
}
