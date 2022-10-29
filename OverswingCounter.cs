using CountersPlus.Counters.Custom;
using CountersPlus.Counters.Interfaces;
using HarmonyLib;
using OverswingCounter.HarmonyPatches;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace OverswingCounter {
    public class OverswingCounter : BasicCustomCounter {
        TMP_Text counterLeftPreswingUp;
		TMP_Text counterRightPreswingUp;

		TMP_Text counterLeftPreswingDown;
		TMP_Text counterRightPreswingDown;

		RollingAverage[] leftValues;
        RollingAverage[] rightValues;

        public override void CounterInit() {
			if(Plugin.harmony == null) {
				Plugin.harmony = new Harmony("Kinsi55.BeatSaber.OverswingCounter");
				Plugin.harmony.PatchAll(Assembly.GetExecutingAssembly());
			}

			// check replay status
			ReplayUtils.Init();
			var isInReplay = ReplayUtils.IsInReplay();
			
            var label = CanvasUtility.CreateTextFromSettings(Settings);
            label.text = "Overswing";
            label.fontSize = 3;

            TMP_Text CreateLabel(TextAlignmentOptions align, Vector3 offset) {
                var x = CanvasUtility.CreateTextFromSettings(Settings, offset);
                x.text = isInReplay ? "-" : FormatDecimals(0f);
                x.alignment = align;

				return x;
            }


			counterRightPreswingUp = CreateLabel(TextAlignmentOptions.TopLeft, new Vector3(0.25f, -0.6f, 0));
			counterLeftPreswingUp = CreateLabel(TextAlignmentOptions.TopRight, new Vector3(-0.25f, -0.6f, 0));

			counterRightPreswingDown = CreateLabel(TextAlignmentOptions.TopLeft, new Vector3(0.25f, -0.2f, 0));
			counterLeftPreswingDown = CreateLabel(TextAlignmentOptions.TopRight, new Vector3(-0.25f, -0.2f, 0));

			//var arrowLabel = CreateLabel(TextAlignmentOptions.TopRight, new Vector3(-0.03f, -0.37f, 0));
			//arrowLabel.text = "▼\n▲";
			//arrowLabel.fontSize = 1.5f;
			//arrowLabel.lineSpacing = 100;

			leftValues = new[] {
                new RollingAverage(Configuration.Instance.averageCount),
                new RollingAverage(Configuration.Instance.averageCount)
            };

            rightValues = new[] {
                new RollingAverage(Configuration.Instance.averageCount),
                new RollingAverage(Configuration.Instance.averageCount)
            };

			GeneralSwingData.newCutCompleted = ProcessCompletedCut;
		}

		public override void CounterDestroy() {
			GeneralSwingData.newCutCompleted = null;
		}


		void ProcessCompletedCut(CutInfo cut) {
			/*
			 * Completely ignore secondary cuts of the same swing, the prime example are stacks. If you underswing,
			 * it will make for multiple underswings. If you dont underswing it will make for multiple overswings
			 * which are not actually overswings because the previous blocks postswing isnt factored in
			 */
			if(!cut.isPrimary)
				return;

			if(cut.lastFinishedCutToCompareAgainst == null) {
				return;
			}

			var previousCutWasWithinTimeframe = 
				Configuration.Instance.ignoreCutsWithNoPrecedingWithin == 0f || 
				cut.cutTime - cut.lastFinishedCutToCompareAgainst.cutTime < Configuration.Instance.ignoreCutsWithNoPrecedingWithin;

			if(!previousCutWasWithinTimeframe)
				return;

			var adjustedBeforeCut = cut.correctedBeforeRating;
			var isLeftSaber = cut.saberType == SaberType.SaberA;



			var isRelatedToPreviousCut = previousCutWasWithinTimeframe && Vector2.Distance(cut.lastFinishedCutToCompareAgainst.swingEnd, cut.swingStart) <= 0.4;

			if(isRelatedToPreviousCut) {
				var prevPostswingExtraAngle = (cut.lastFinishedCutToCompareAgainst.correctedAfterRating - 1) * SaberSwingRating.kAfterCutAngleFor1Rating;

				/*
				 * Based on the extra postswing angle, calculate how much % of a PRESWING that is, to compare it to
				 * this cuts preswing and see which one should be used to decide the overswing
				 */
				var extraPreswingAsPostswingFrac = 1f + (prevPostswingExtraAngle / SaberSwingRating.kBeforeCutAngleFor1Rating);

#if DEBUG
				Console.WriteLine("Previous cut post-swing, converted to pre swing: {0:P2}", extraPreswingAsPostswingFrac);
#endif
				// We wanna use whatever is lower, either the previous postswing, or our pre swing
				if(extraPreswingAsPostswingFrac < adjustedBeforeCut) {
#if DEBUG
					Console.WriteLine("Previous had a smaller effective preswing. Using that instead.");
#endif
					adjustedBeforeCut = extraPreswingAsPostswingFrac;
				}
			} else if(previousCutWasWithinTimeframe) {
#if DEBUG
				Console.WriteLine("Previous cut with this saber was unrelated - Accounting for its postswing");
#endif
				/*
				* If the previous cut is NOT related to our current cut we need to factor in its postswing
				* as a theoretical preswing. This should probably primarily only happen with DD's
				*/
				GetCounterAndLabel(
					isLeftSaber,
					cut.lastFinishedCutToCompareAgainst.isDownswing ? 1 : 0,
					out var targetAvgPrev, out var labelPrev
				);

				targetAvgPrev.Add((cut.lastFinishedCutToCompareAgainst.correctedAfterRating - 1) * SaberSwingRating.kAfterCutAngleFor1Rating);
				SetLabelValue(targetAvgPrev, labelPrev);
			}



			GetCounterAndLabel(
				isLeftSaber,
				cut.isDownswing ? 0 : 1,
				out var targetAvg, out var label
			);

			targetAvg.Add((adjustedBeforeCut - 1f) * SaberSwingRating.kBeforeCutAngleFor1Rating);
			SetLabelValue(targetAvg, label);
#if DEBUG
			Console.WriteLine(" ");
#endif
		}

		void GetCounterAndLabel(bool leftSaber, int index, out RollingAverage avg, out TMP_Text label) {
			avg = leftSaber ? leftValues[index] : rightValues[index];
			label = leftSaber ? (index == 0 ? counterLeftPreswingDown : counterLeftPreswingUp) : (index == 0 ? counterRightPreswingDown : counterRightPreswingUp);
		}

		void SetLabelValue(RollingAverage v, TMP_Text label) {
			label.text = FormatDecimals((float)v.average) + "°";

			var ptsDeviation = v.average - Configuration.Instance.targetExtraAngle;

			var colorValue = ptsDeviation;
			var outColor = Color.red;

			if(colorValue >= 0) {
				colorValue /= Configuration.Instance.upperWarning;
				outColor = Color.yellow;
			} else {
				colorValue /= -Configuration.Instance.lowerWarning;
			}

			label.color = Color.Lerp(Color.white, outColor, (float)Math.Pow(colorValue, 3));
		}

		string FormatDecimals(float v) => v.ToString($"F{Configuration.Instance.decimalPlaces}", CultureInfo.InvariantCulture);
    }
}
