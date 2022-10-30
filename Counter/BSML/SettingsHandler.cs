using BeatSaberMarkupLanguage.Attributes;
using OverswingCounter.Configuration;

namespace OverswingCounter.Counter.BSML
{
    internal class SettingsHandler
    {
        [UIValue("Config")]
        public PluginConfig Config => PluginConfig.Instance;
    }
}