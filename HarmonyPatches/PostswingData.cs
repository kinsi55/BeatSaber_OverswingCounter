using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace OverswingCounter.HarmonyPatches {
	[HarmonyPatch(typeof(SaberSwingRatingCounter), nameof(SaberSwingRatingCounter.ProcessNewData))]
	static class GetUnclampedPostSwingData {
		static MethodBase SaberSwingRating_AfterCutStepRating = AccessTools.Method(typeof(SaberSwingRating), nameof(SaberSwingRating.AfterCutStepRating));
		static FieldInfo SaberSwingRatingCounter_afterCutRating = AccessTools.Field(typeof(SaberSwingRatingCounter), "_afterCutRating");

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
			var res = instructions.ToList();

			bool waitForStfld = false;

			var interceptor = new [] {
				new CodeInstruction(OpCodes.Ldarg_0), // this

				new CodeInstruction(OpCodes.Ldarg_0), // this._afterCutRating
				new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(SaberSwingRatingCounter), "_afterCutRating")),

				new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GetUnclampedPostSwingData), nameof(AccumulatorFn)))
			};

			for(var idx = 0; idx < res.Count; idx++) {
				var elem = res[idx];

				if(waitForStfld) {
					if(elem.opcode != OpCodes.Stfld || elem.operand != (object)SaberSwingRatingCounter_afterCutRating)
						continue;
						
					res.InsertRange(idx + 1, interceptor);
					idx += interceptor.Length;

					waitForStfld = false;
				} else if(elem.opcode == OpCodes.Call && elem.operand == (object)SaberSwingRating_AfterCutStepRating) {
					waitForStfld = true;
				}
			}

			return res;
		}

		public static void AccumulatorFn(SaberSwingRatingCounter __instance, float tmpRating) {
			if(!GeneralSwingData.swingRatingInfos.TryGetValue(__instance, out var info) || info.counter == null)
				return;

			if(tmpRating <= 1f) {
				info.afterRating = tmpRating;
			} else {
				info.afterRating += tmpRating - 1f;
			}
		}
	}
}
