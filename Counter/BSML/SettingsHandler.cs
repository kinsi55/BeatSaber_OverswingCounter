using BeatSaberMarkupLanguage.Attributes;
using OverswingCounter.Configuration;

namespace OverswingCounter.Counter.BSML {
	internal class SettingsHandler {
		[UIValue("Config")]
		public Config Config => Config.Instance;
	}
}