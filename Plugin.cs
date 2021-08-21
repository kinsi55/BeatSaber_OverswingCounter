using HarmonyLib;
using IPA;
using IPA.Config;
using IPA.Config.Stores;
using OverswingCounter.HarmonyPatches;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using IPALogger = IPA.Logging.Logger;

namespace OverswingCounter {
	[Plugin(RuntimeOptions.SingleStartInit)]
	public class Plugin {
		internal static Plugin Instance { get; private set; }
		internal static IPALogger Log { get; private set; }
		internal static Harmony harmony = null;

		internal static string Name => "OverswingCounter";

		[Init]
		public void Init(IPALogger logger, IPA.Config.Config config) {
			Configuration.Instance = config.Generated<Configuration>();
			Instance = this;
			Log = logger;
		}

		#region BSIPA Config
		//Uncomment to use BSIPA's config
		/*
        [Init]
        public void InitWithConfig(Config conf)
        {
            Configuration.PluginConfig.Instance = conf.Generated<Configuration.PluginConfig>();
            Log.Debug("Config loaded");
        }
        */
		#endregion

		[OnStart]
		public void OnApplicationStart() {
			SceneManager.activeSceneChanged += OnActiveSceneChanged;
		}

		public static void OnActiveSceneChanged(Scene oldScene, Scene newScene) {
			if(oldScene.name == "GameCore" || newScene.name == "GameCore")
				GeneralSwingData.Clear();
		}

		[OnExit]
		public void OnApplicationQuit() {
			harmony?.UnpatchAll(harmony.Id);
		}
	}
}
