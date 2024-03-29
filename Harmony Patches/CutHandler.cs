﻿using System;
using System.Collections.Generic;
using HarmonyLib;
using OverswingCounter.Configuration;
using OverswingCounter.Models;

namespace OverswingCounter.Harmony_Patches {
	[HarmonyPatch(typeof(GoodCutScoringElement), nameof(GoodCutScoringElement.Init))]
	internal static class CutHandler {
		internal static Dictionary<SaberType, CutInfo> CurrentPrimaryCut;
		internal static Dictionary<SaberType, CutInfo> LastFinishedCut;
		internal static Action<CutInfo> NewCutCompleted;

		internal static void Clear() {
			CurrentPrimaryCut = new Dictionary<SaberType, CutInfo>(2)
			{
				{ SaberType.SaberA, null },
				{ SaberType.SaberB, null }
			};
			LastFinishedCut = new Dictionary<SaberType, CutInfo>(2)
			{
				{ SaberType.SaberA, null },
				{ SaberType.SaberB, null }
			};
		}

		[HarmonyPrefix]
		internal static void Prefix(ref NoteCutInfo noteCutInfo) {
			if(Config.Instance.ignoreArcsAndChains &&
				noteCutInfo.noteData.scoringType is NoteData.ScoringType.SliderHead
					or NoteData.ScoringType.SliderTail
					or NoteData.ScoringType.BurstSliderHead
					or NoteData.ScoringType.BurstSliderElement)
				return;

			new CutInfo(noteCutInfo);
		}
	}
}