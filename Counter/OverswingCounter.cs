using System;
using System.Globalization;
using CountersPlus.Counters.Custom;
using OverswingCounter.Configuration;
using OverswingCounter.Harmony_Patches;
using OverswingCounter.Models;
using TMPro;
using UnityEngine;

namespace OverswingCounter.Counter
{
    public class OverswingCounter : BasicCustomCounter
    {
        private TMP_Text _counterLeftPreswingUp;
        private TMP_Text _counterRightPreswingUp;
        
        private TMP_Text _counterLeftPreswingDown;
        private TMP_Text _counterRightPreswingDown;

        private RollingAverage[] _leftValues;
        private RollingAverage[] _rightValues;
        
        public override void CounterInit()
        {
            var label = CanvasUtility.CreateTextFromSettings(Settings);
            label.text = "Overswing";
            label.fontSize = 3;

            TMP_Text CreateLabel(TextAlignmentOptions align, Vector3 offset) {
                var x = CanvasUtility.CreateTextFromSettings(Settings, offset);
                x.text = FormatDecimals(0f);
                x.alignment = align;

                return x;
            }

            _counterRightPreswingUp = CreateLabel(TextAlignmentOptions.TopLeft, new Vector3(0.25f, -0.6f, 0));
            _counterLeftPreswingUp = CreateLabel(TextAlignmentOptions.TopRight, new Vector3(-0.25f, -0.6f, 0));

            _counterRightPreswingDown = CreateLabel(TextAlignmentOptions.TopLeft, new Vector3(0.25f, -0.2f, 0));
            _counterLeftPreswingDown = CreateLabel(TextAlignmentOptions.TopRight, new Vector3(-0.25f, -0.2f, 0));

            _leftValues = new[] {
                new RollingAverage(Config.Instance.averageCount),
                new RollingAverage(Config.Instance.averageCount)
            };

            _rightValues = new[] {
                new RollingAverage(Config.Instance.averageCount),
                new RollingAverage(Config.Instance.averageCount)
            };

            CutHandler.NewCutCompleted = ProcessCompletedCut;
        }

        public override void CounterDestroy()
        {
            
        }
        
        private void ProcessCompletedCut(CutInfo cut)
        {
            if (!cut.IsPrimary || cut.LastFinishedCutToCompareAgainst == null)
				return;

            var previousCutWasWithinTimeframe = 
				Config.Instance.ignoreCutsWithNoPrecedingWithin == 0f || 
				cut.CutTime - cut.LastFinishedCutToCompareAgainst.CutTime < Config.Instance.ignoreCutsWithNoPrecedingWithin;

			if(!previousCutWasWithinTimeframe)
				return;

			var adjustedBeforeCut = cut.BeforeRating;
			var isLeftSaber = cut.SaberType == SaberType.SaberA;



			var isRelatedToPreviousCut = previousCutWasWithinTimeframe && Vector2.Distance(cut.LastFinishedCutToCompareAgainst.EndPos, cut.StartPos) <= 0.4;

			if (isRelatedToPreviousCut) 
            {
				var prevPostswingExtraAngle = (cut.LastFinishedCutToCompareAgainst.AfterRating - 1) * SaberSwingRating.kAfterCutAngleFor1Rating;

				/*
				 * Based on the extra postswing angle, calculate how much % of a PRESWING that is, to compare it to
				 * this cuts preswing and see which one should be used to decide the overswing
				 */
				var extraPreswingAsPostswingFrac = 1f + (prevPostswingExtraAngle / SaberSwingRating.kBeforeCutAngleFor1Rating);

#if DEBUG
				Console.WriteLine("Previous cut post-swing, converted to pre swing: {0:P2}", extraPreswingAsPostswingFrac);
#endif
				// We wanna use whatever is lower, either the previous postswing, or our pre swing
				if (extraPreswingAsPostswingFrac < adjustedBeforeCut) 
                {
#if DEBUG
					Console.WriteLine("Previous had a smaller effective preswing. Using that instead.");
#endif
					adjustedBeforeCut = extraPreswingAsPostswingFrac;
				}
			} 
            else if(previousCutWasWithinTimeframe) 
            {
#if DEBUG
				Console.WriteLine("Previous cut with this saber was unrelated - Accounting for its postswing");
#endif
				/*
				* If the previous cut is NOT related to our current cut we need to factor in its postswing
				* as a theoretical preswing. This should probably primarily only happen with DD's
				*/
				GetCounterAndLabel(
					isLeftSaber,
					cut.LastFinishedCutToCompareAgainst.IsDownswing ? 1 : 0,
					out var targetAvgPrev, out var labelPrev
				);

				targetAvgPrev.Add((cut.LastFinishedCutToCompareAgainst.AfterRating - 1) * SaberSwingRating.kAfterCutAngleFor1Rating);
				SetLabelValue(targetAvgPrev, labelPrev);
			}



			GetCounterAndLabel(
				isLeftSaber,
				cut.IsDownswing ? 0 : 1,
				out var targetAvg, out var label
			);

			targetAvg.Add((adjustedBeforeCut - 1f) * SaberSwingRating.kBeforeCutAngleFor1Rating);
			SetLabelValue(targetAvg, label);
        }
        
        private void GetCounterAndLabel(bool leftSaber, int index, out RollingAverage avg, out TMP_Text label) {
            avg = leftSaber ? _leftValues[index] : _rightValues[index];
            label = leftSaber ? (index == 0 ? _counterLeftPreswingDown : _counterLeftPreswingUp) : (index == 0 ? _counterRightPreswingDown : _counterRightPreswingUp);
        }

        private void SetLabelValue(RollingAverage v, TMP_Text label) {
            label.text = FormatDecimals((float)v.Average) + "°";

            var ptsDeviation = v.Average - Config.Instance.targetExtraAngle;

            var colorValue = ptsDeviation;
            var outColor = Color.red;

            if (colorValue >= 0) 
            {
                colorValue /= Config.Instance.upperWarning;
                outColor = Color.yellow;
            } 
            else 
            {
                colorValue /= -Config.Instance.lowerWarning;
            }

            label.color = Color.Lerp(Color.white, outColor, (float)Math.Pow(colorValue, 3));
        }

        private string FormatDecimals(float f) => f.ToString($"F{Config.Instance.decimalPlaces}", CultureInfo.InvariantCulture);
    }
}