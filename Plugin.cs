using System.Linq;
using System.Reflection;
using HarmonyLib;
using IPA;
using IPA.Config.Stores;
using OverswingCounter.Configuration;
using OverswingCounter.Harmony_Patches;
using UnityEngine;
using UnityEngine.SceneManagement;
using IPALogger = IPA.Logging.Logger;

namespace OverswingCounter
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    public class Plugin
    {
        internal static Plugin Instance { get; private set; }
        internal static IPALogger Log { get; private set; }
        internal static Harmony Harmony;
        internal static SaberManager SaberManager;

        [Init]
        public void Init(IPALogger logger, IPA.Config.Config config)
        {
            Instance = this;
            Log = logger;
            Config.Instance = config.Generated<Config>();
            Harmony = new Harmony("Kinsi55.BeatSaber.OverswingCounter");
        }

        [OnEnable]
        public void OnEnable()
        {
            SceneManager.activeSceneChanged += delegate(Scene oldScene, Scene newScene)
            {
                if (oldScene.name != "GameCore" && newScene.name != "GameCore") return;

                SaberManager = Resources.FindObjectsOfTypeAll<SaberManager>().First();
                CutHandler.Clear();
            };
            
            Harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        [OnDisable]
        public void OnDisable()
        {
            Harmony.UnpatchSelf();
        }
    }
}