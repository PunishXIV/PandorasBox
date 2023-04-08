using Dalamud.Logging;
using Dalamud.Plugin;
using ECommons.Reflection;
using Newtonsoft.Json;
using PandorasBox.FeaturesSetup;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PandorasBox.Features
{
    public abstract class BaseFeature
    {
        protected PandorasBox P;
        protected DalamudPluginInterface Pi;
        protected Configuration config;
        public FeatureProvider Provider { get; private set; } = null!;

        public virtual bool Enabled { get; protected set; } 

        public virtual string Name { get; }

        public virtual string Key => GetType().Name;

        public virtual string Description => null!;

        public virtual void Draw()
        {

        }

        public virtual bool Ready { get; protected set; }

        public virtual FeatureType FeatureType { get; }

        public void InterfaceSetup(PandorasBox plugin, DalamudPluginInterface pluginInterface, Configuration config, FeatureProvider fp)
        {
            this.P = plugin;
            this.Pi = pluginInterface;
            this.config = config;
            this.Provider = fp;
        }

        public virtual void Setup()
        {
            Ready = true;
        }

        public virtual void Enable()
        {
            Enabled = true;
        }

        public virtual void Disable()
        {
            Enabled = false;
        }

        public virtual void Dispose()
        {
            Ready = false;
        }

        protected T LoadConfig<T>() where T : FeatureConfig => LoadConfig<T>(this.Key);

        protected T LoadConfig<T>(string key) where T : FeatureConfig
        {
            try
            {
                var configDirectory = pi.GetPluginConfigDirectory();
                var configFile = Path.Combine(configDirectory, key + ".json");
                if (!File.Exists(configFile)) return default;
                var jsonString = File.ReadAllText(configFile);
                return JsonConvert.DeserializeObject<T>(jsonString);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Failed to load config for feature {Name}");
                return default;
            }
        }

        protected void SaveConfig<T>(T config) where T : FeatureConfig
        {
            try
            {
                var configDirectory = pi.GetPluginConfigDirectory();
                var configFile = Path.Combine(configDirectory, this.Key + ".json");
                var jsonString = JsonConvert.SerializeObject(config, Formatting.Indented);

                File.WriteAllText(configFile, jsonString);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Feature failed to write config {this.Name}");
            }
        }
    }
}
