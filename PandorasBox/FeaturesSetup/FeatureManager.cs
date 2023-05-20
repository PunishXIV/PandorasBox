using Dalamud.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PandorasBox.Features
{
    public abstract class FeatureManager : Feature
    {
        public abstract List<BaseFeature> GetFeatures();

    }

    public abstract class FeatureManager<T> : FeatureManager where T : BaseFeature
    {
        public List<BaseFeature> BaseFeatures = new();

        public override List<BaseFeature> GetFeatures()
        {
            return BaseFeatures.Cast<BaseFeature>().ToList();
        }

        public string GetFeatureKey(T t)
        {
            return $"{GetType().Name}@{t.GetType().Name}";
        }

        public override void Setup()
        {
            var featureList = new List<BaseFeature>();

            foreach (var t in GetType().Assembly.GetTypes().Where(t => t.IsSubclassOf(typeof(T))))
            {
                try
                {
                    var feature = (T)Activator.CreateInstance(t);
                    if (feature == null) continue;
                    feature.InterfaceSetup(this.P, this.Pi, this.config, this.Provider);
                    feature.Setup();

                    featureList.Add(feature);
                }
                catch (Exception ex)
                {
                    PluginLog.Error(ex, $"Error in Setup of '{t.Name}' @ '{this.Name}'");
                }
            }

            BaseFeatures = featureList.OrderBy(t => t.Name).ToList();
        }
    }
}
