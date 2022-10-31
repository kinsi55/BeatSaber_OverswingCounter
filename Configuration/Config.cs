using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace OverswingCounter.Configuration {
	internal class Config {
		public static Config Instance;

		public virtual int averageCount { get; set; } = 12;
		public virtual int decimalPlaces { get; set; } = 0;
		public virtual bool underswingByPoints { get; set; } = true;

		public virtual float ignoreCutsWithNoPrecedingWithin { get; set; } = 0.6f;

		public virtual float targetExtraAngle { get; set; } = 13f;
		public virtual float lowerWarning { get; set; } = 8f;
		public virtual float upperWarning { get; set; } = 13f;

		public virtual bool ignoreArcsAndChains { get; set; } = true;
	}
}