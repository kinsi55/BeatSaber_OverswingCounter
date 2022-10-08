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

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
			var res = instructions.ToList();
			
			var foo = 0;

			var interceptor = new [] {
				new CodeInstruction(OpCodes.Dup),
				new CodeInstruction(OpCodes.Ldarg_0),
				null,
				new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(GetUnclampedPostSwingData), nameof(AccumulatorFn)))
			};

			for(var idx = 0; idx < res.Count; idx++) {
				var elem = res[idx];

				if(elem.opcode == OpCodes.Call && elem.operand == (object)SaberSwingRating_AfterCutStepRating) {
					interceptor[2] = new CodeInstruction(OpCodes.Ldc_I4, foo++);
					res.InsertRange(idx + 1, interceptor);
				}
			}

			return res;
		}

		public static void AccumulatorFn(float tmpRating, SaberSwingRatingCounter __instance, int num) {
			if(!GeneralSwingData.swingRatingInfos.TryGetValue(__instance, out var info) || info.counter == null)
				return;

			if(num == 0) {
				info.afterRating = tmpRating;
			} else {
				info.afterRating += tmpRating;
			}
		}
	}
}
