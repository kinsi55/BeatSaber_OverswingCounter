
using System.Runtime.CompilerServices;
using IPA.Config.Stores;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]
namespace OverswingCounter {
	internal class Configuration {
		public static Configuration Instance { get; set; }

		public virtual int averageCount { get; set; } = 12;
		public virtual int decimalPlaces { get; set; } = 0;
		public virtual bool underswingByPoints { get; set; } = true;

		public virtual float ignoreCutsWithNoPrecedingWithin { get; set; } = 0.6f;
		
		public virtual float targetExtraAngle { get; set; } = 13f;
		public virtual float lowerWarning { get; set; } = 8f;
		public virtual float upperWarning { get; set; } = 13f;

		public virtual bool ignoreArcsAndChains { get; set; } = true;


		/// <summary>
		/// This is called whenever BSIPA reads the config from disk (including when file changes are detected).
		/// </summary>
		public virtual void OnReload() {
			// Do stuff after config is read from disk.
		}

		/// <summary>
		/// Call this to force BSIPA to update the config file. This is also called by BSIPA if it detects the file was modified.
		/// </summary>
		public virtual void Changed() {
			// Do stuff when the config is changed.
		}

		/// <summary>
		/// Call this to have BSIPA copy the values from <paramref name="other"/> into this config.
		/// </summary>
		public virtual void CopyFrom(Configuration other) {
			// This instance's members populated from other
		}
	}
}
