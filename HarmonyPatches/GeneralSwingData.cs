using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;

namespace OverswingCounter.HarmonyPatches {
	[HarmonyPatch(typeof(CutScoreBuffer), nameof(CutScoreBuffer.Init))]
	static class GeneralSwingData {
		public static Dictionary<SaberType, CutInfo> currentPrimaryCut;
		public static Dictionary<SaberType, CutInfo> lastFinishedCut;

		[HarmonyPriority(int.MaxValue)]
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
			var res = instructions.ToList();

			res.InsertRange(65, new[] {
				// get SSRC instance
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CutScoreBuffer), "_saberSwingRatingCounter")),
				
				// get saber type
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(CutScoreBuffer), "_noteCutInfo")),
				new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(NoteCutInfo), "saberType")),
				
				new CodeInstruction(OpCodes.Ldloc_0), // rateBefore
				new CodeInstruction(OpCodes.Ldloc_1), // rateAfter

				new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GeneralSwingData), nameof(PrepareNewSwingRatingCounter)))
			});

			return res;
		}

		public static Dictionary<SaberSwingRatingCounter, CutInfo> swingRatingInfos { get; private set; } = new Dictionary<SaberSwingRatingCounter, CutInfo>();

		static void PrepareNewSwingRatingCounter(SaberSwingRatingCounter counter, SaberType saberType, bool rateBefore, bool rateAfter) {
#if TRACE
			Console.WriteLine("GameNoteController.HandleCut => PrepareNewSwingRatingCounter()");
#endif

			if(!swingRatingInfos.TryGetValue(counter, out var existing) || existing.counter != counter)
				swingRatingInfos[counter] = new CutInfo(counter, SaberManager.SaberForType(saberType), rateBefore, rateAfter);
		}

		public static void Clear() {
			currentPrimaryCut = new Dictionary<SaberType, CutInfo>(2) {
				{ SaberType.SaberA, null },
				{ SaberType.SaberB, null },
			};

			lastFinishedCut = new Dictionary<SaberType, CutInfo>(2) {
				{ SaberType.SaberA, null },
				{ SaberType.SaberB, null },
			};

			swingRatingInfos.Clear();

			SaberManager = Resources.FindObjectsOfTypeAll<SaberManager>().FirstOrDefault();
		}

		public static Action<CutInfo> newCutCompleted;
		public static SaberManager SaberManager;
	}

	public class CutInfo : ISaberSwingRatingCounterDidFinishReceiver {
		public SaberSwingRatingCounter counter;
		public SaberType saberType;
		public Saber saber;

		public float beforeRating = 0f;
		public float correctedBeforeRating = 0f;
		public float afterRating = 0f;
		public float correctedAfterRating = 0f;
		public float angle = 0f;

		public bool rateBefore;
		public bool rateAfter;

		//public Vector2 notePosition2D;

		public Vector2 swingStart;
		public Vector2 swingEnd;

		public float cutTime;

		public bool isDownswing => angle <= 10f || angle >= 170f;


		public bool isPrimary { get; private set; } = false;
		public CutInfo lastFinishedCutToCompareAgainst { get; private set; }

		public CutInfo(SaberSwingRatingCounter counter, Saber saber, bool rateBefore, bool rateAfter) {
			this.counter = counter;
			this.saber = saber;
			saberType = saber.saberType;
			cutTime = Time.realtimeSinceStartup;
			this.rateBefore = rateBefore;
			this.rateAfter = rateAfter;
			//notePosition2D = new Vector2(notePosition.x, notePosition.y);

			lastFinishedCutToCompareAgainst = GeneralSwingData.lastFinishedCut[saberType];

			isPrimary = GeneralSwingData.currentPrimaryCut[saberType] == null;
			if(isPrimary)
				GeneralSwingData.currentPrimaryCut[saberType] = this;

			counter.RegisterDidFinishReceiver(this);
		}

		public void HandleSaberSwingRatingCounterDidFinish(ISaberSwingRatingCounter saberSwingRatingCounter) {
			counter.UnregisterDidFinishReceiver(this);
			swingEnd = saber.saberBladeTopPos;
#if DEBUG
			Console.WriteLine("Counter {0} (Primary: {5}) finished ({4})! {1:P2}pre, {2:P2}post, {3}angle (isDown: {6})", counter.GetHashCode(), beforeRating, afterRating, angle, saberType, isPrimary, isDownswing);
#endif
			counter = null;

			if (!rateBefore) beforeRating = 1f;
			if (!rateAfter) afterRating = 1f;
			
			/*
			 * When you are overswinging, you are always overswinging the exact angle that you do overswing,
			 * but underswinging is a special case where you can *theoretically* be underswinging, but in reality,
			 * due to swing points being rounded per cut, you might still be getting full points. We kinda need to
			 * account for that and calculate your theoretical swing angle based off the points you got
			 */
			correctedAfterRating = afterRating;
			if(afterRating < 1f) {
				var cCorrectedAfterRating = (float)Math.Round(afterRating * SaberSwingRating.kAfterCutAngleFor1Rating) / SaberSwingRating.kAfterCutAngleFor1Rating;

				// We only want to round UP, not down
				if(cCorrectedAfterRating > afterRating)
					correctedAfterRating = cCorrectedAfterRating;
			}

			correctedBeforeRating = beforeRating;
			if(beforeRating < 1f) {
				var cCorrectedBeforeRating = (float)Math.Round(beforeRating * SaberSwingRating.kBeforeCutAngleFor1Rating) / SaberSwingRating.kBeforeCutAngleFor1Rating;

				if(cCorrectedBeforeRating > beforeRating)
					correctedBeforeRating = cCorrectedBeforeRating;
			}
			
			GeneralSwingData.lastFinishedCut[saberType] = this;

			if(GeneralSwingData.currentPrimaryCut[saberType] == this)
				GeneralSwingData.currentPrimaryCut[saberType] = null;

			if(GeneralSwingData.newCutCompleted != null)
				GeneralSwingData.newCutCompleted(this);
		}
	}
}
