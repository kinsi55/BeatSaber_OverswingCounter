using BeatSaberMarkupLanguage.Attributes;
using System.Collections.Generic;
using System.Linq;

namespace OverswingCounter {
    class SettingsHandler {
        public int averageCount {
            get => Configuration.Instance.averageCount;
            set => Configuration.Instance.averageCount = value;
        }
        public int decimalPlaces {
            get => Configuration.Instance.decimalPlaces;
            set => Configuration.Instance.decimalPlaces = value;
        }

        public float ignoreCutsWithNoPrecedingWithin {
            get => Configuration.Instance.ignoreCutsWithNoPrecedingWithin;
            set => Configuration.Instance.ignoreCutsWithNoPrecedingWithin = value;
        }

        public float targetExtraAngle {
            get => Configuration.Instance.targetExtraAngle;
            set => Configuration.Instance.targetExtraAngle = value;
        }
        public float lowerWarning {
            get => Configuration.Instance.lowerWarning;
            set => Configuration.Instance.lowerWarning = value;
        }
        public float upperWarning {
            get => Configuration.Instance.upperWarning;
            set => Configuration.Instance.upperWarning = value;
        }
        public bool ignoreArcsAndChains {
            get => Configuration.Instance.ignoreArcsAndChains;
            set => Configuration.Instance.ignoreArcsAndChains = value;
        }
    }
}