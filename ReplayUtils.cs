using System;
using System.Reflection;
using IPA.Loader;

namespace OverswingCounter {
    public static class ReplayUtils {
        private static MethodInfo _scoreSaber;
        private static PropertyInfo _beatLeader;
        
        public static bool lastStatus { get; private set; }

        public static void Init() {
            var scoreSaber = PluginManager.GetPluginFromId("ScoreSaber");
            if (scoreSaber == null) goto beatLeader;
            _scoreSaber = scoreSaber.Assembly.GetType("ScoreSaber.Core.ReplaySystem.HarmonyPatches.PatchHandleHMDUnmounted")
                .GetMethod("Prefix", BindingFlags.Static | BindingFlags.NonPublic);
            
            beatLeader:
            var beatLeader = PluginManager.GetPluginFromId("BeatLeader");
            if (beatLeader == null) return;
            _beatLeader = beatLeader.Assembly.GetType("BeatLeader.Replayer.ReplayerLauncher")
                .GetProperty("IsStartedAsReplay", BindingFlags.Static | BindingFlags.Public);
        }

        public static bool IsInReplay() {
            bool Check() {
                bool ss = false, bl = false;
                if (_scoreSaber != null) {
                    try {
                        ss = !(bool)_scoreSaber.Invoke(null, null);
                    }
                    catch (Exception ex) {
                        Plugin.Log.Error(ex);
                    }
                }

                if (_beatLeader != null) {
                    try {
                        bl = (bool)_beatLeader.GetValue(null, null);
                    }
                    catch (Exception ex) {
                        Plugin.Log.Error(ex);
                    }
                }

                return ss || bl;
            }

            lastStatus = Check();
            return lastStatus;
        }
    }
}