using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace OverswingCounter.HarmonyPatches {
	[HarmonyPatch]
	static class PreswingData {
		static IEnumerable<MethodBase> TargetMethods() {
			yield return AccessTools.Method(typeof(SaberSwingRatingCounter), nameof(SaberSwingRatingCounter.Init));
			yield return AccessTools.Method(typeof(SaberSwingRatingCounter), nameof(SaberSwingRatingCounter.ProcessNewData));
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
			var res = instructions.ToList();

			var _CalculatedNewPreswingValue = AccessTools.Method(typeof(PreswingData), nameof(CalculatedNewPreswingValue));

			for(var i = 0; i < res.Count; i++) {
				var ci = res[i];

				// Checking vs method name because theres overloads
				if(ci.opcode != OpCodes.Callvirt || ((MethodBase)ci.operand).Name != "ComputeSwingRating")
					continue;

				i++;

				res.InsertRange(i, new[] {
					new CodeInstruction(OpCodes.Ldarg_0).MoveLabelsFrom(res[i]),
					new CodeInstruction(OpCodes.Call, _CalculatedNewPreswingValue)
				});
			}

			return res;
		}

		static void CalculatedNewPreswingValue(SaberSwingRatingCounter __instance) {
			if(!GeneralSwingData.swingRatingInfos.TryGetValue(__instance, out var info) || info.counter == null)
				return;

			info.beforeRating = LastCalculatedPreswingUnclamped.beforeCutRatingUnclamped;
			info.angle = LastCalculatedPreswingUnclamped.beforeCutPlaneAngle;
			info.swingStart = LastCalculatedPreswingUnclamped.beforeCutStartPos;
		}
	}



	[HarmonyPatch(typeof(SaberMovementData), nameof(SaberMovementData.ComputeSwingRating), new [] { typeof(bool), typeof(float) })]
	static class LastCalculatedPreswingUnclamped {
		static float beforeCutRatingUnclampedf = 0;
		static float beforeCutPlaneAngleF;
		public static Vector3 beforeCutStartPos;
		public static Vector3 beforeCutEntryPos;

		static public float beforeCutRatingUnclamped => beforeCutRatingUnclampedf;
		static public float beforeCutPlaneAngle => beforeCutPlaneAngleF;

		//static public float beforeCutPlaneAngleModded;

		static void CalcAngle(Vector3 startPos, Vector3 endPos) {
			beforeCutStartPos = startPos;
			beforeCutEntryPos = endPos;

			// -90 = down, 90 = up
			beforeCutPlaneAngleF = Mathf.Rad2Deg * Mathf.Atan2(endPos.y - startPos.y, endPos.x - startPos.x);

			//// 270 = down, 90 = up
			//if(beforeCutPlaneAngleModded < 0)
			//	beforeCutPlaneAngleModded += 360;

			//// 0 down, 180 up
			//beforeCutPlaneAngleModded = (beforeCutPlaneAngleModded + 90) % 360;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
			var res = instructions.ToList();

			var vTipEndPos = il.DeclareLocal(typeof(Vector3)).LocalIndex;
			var vTipStartPos = il.DeclareLocal(typeof(Vector3)).LocalIndex;

			// instruction before > 1f comparison (ldloc_s 4, ldc_r4, ble_un_s)
			var extractPos = res.FindLastIndex(x => x.opcode == OpCodes.Blt_S || x.opcode == OpCodes.Blt);

			if(extractPos == -1)
				throw new Exception("Broky");

			extractPos++;

			res.InsertRange(extractPos, new[] {
				new CodeInstruction(OpCodes.Ldloc_S, 4).MoveLabelsFrom(res[extractPos]), // num4
				new CodeInstruction(OpCodes.Stsfld, AccessTools.Field(typeof(LastCalculatedPreswingUnclamped), nameof(beforeCutRatingUnclampedf))),

				new CodeInstruction(OpCodes.Ldloc_S, vTipStartPos),
				new CodeInstruction(OpCodes.Ldloc_S, vTipEndPos),
				new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(LastCalculatedPreswingUnclamped), nameof(CalcAngle)))
			});

			var ldTopPos = new[] {
				new CodeInstruction(OpCodes.Ldarg_0),
				new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(SaberMovementData), "_data")),

				new CodeInstruction(OpCodes.Ldloc_1),
				new CodeInstruction(OpCodes.Ldelema, typeof(BladeMovementDataElement)),

				new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(BladeMovementDataElement), nameof(BladeMovementDataElement.topPos)))
			};

			// Store the oldest valid tip position for this preswing (After the break if num6 > 90f)
			res.InsertRange(86, ldTopPos.Append(new CodeInstruction(OpCodes.Stloc_S, vTipStartPos)).ToArray());
			// Store the current (newest) tip position for this preswing (Can be anywhere up there as long as num2 is correctly initialized)
			res.InsertRange(49, ldTopPos.Append(new CodeInstruction(OpCodes.Stloc_S, vTipEndPos)).ToArray());

			return res;
		}
	}
}
