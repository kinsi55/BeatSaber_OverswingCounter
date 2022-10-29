/*
 * Prototype for measuring overswing on arcs & chains
 *
 * It removes all if statements which check if it should rate the before or after cuts
 * 
 * Correct value will be set in SaberSwingRatingCounter::Finish
 *   and at the end of SaberSwingRatingCounter::ProcessNewData
 */

using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;

namespace OverswingCounter.HarmonyPatches {
    [HarmonyPatch(typeof(SaberSwingRatingCounter))]
    static class UnlockRatings {
        
        /*
           Original code:
                if (rateBeforeCut) {
					this._beforeCutRating = saberMovementData.ComputeSwingRating();
				} else {
					this._beforeCutRating = 1f;
				}
				
				if (rateAfterCut) {
					this._afterCutRating = 0f;
				} else {
					this._afterCutRating = 1f;
				}
				
			New code:
				this._beforeCutRating = saberMovementData.ComputeSwingRating();
				this._afterCutRating = 0f;
         */
        [HarmonyPatch(nameof(SaberSwingRatingCounter.Init))]
        [HarmonyTranspiler]
        [HarmonyPriority(int.MaxValue)]
        static IEnumerable<CodeInstruction> Init_Transpiler(IEnumerable<CodeInstruction> instructions) {
            var res = instructions.ToList();

            var findNextBrS = false;
            for (var i = 0; i < res.Count; i++) {
                var ci = res[i];

                if (findNextBrS && ci.opcode == OpCodes.Br_S) {
                    res[i + 4].MoveLabelsFrom(res[i]);
                    res.RemoveRange(i, 4);
                    i -= 4;
                    findNextBrS = false;
                } else if (ci.opcode == OpCodes.Ldarg_S
                          && ((byte)ci.operand == 4 || (byte)ci.operand == 5)
                          && (res[i + 1].opcode == OpCodes.Brfalse || res[i + 1].opcode == OpCodes.Brfalse_S)) {
                    res[i + 2].MoveLabelsFrom(res[i]);
                    res.RemoveRange(i, 2);
                    findNextBrS = true;
                }
            }
            
            return res;
        }

        [HarmonyPatch(nameof(SaberSwingRatingCounter.ProcessNewData))]
        [HarmonyTranspiler]
        [HarmonyPriority(int.MaxValue)]
        static IEnumerable<CodeInstruction> ProcessNewData_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il) {
	        var res = instructions.ToList();

	        var fieldBeforeCutRating = AccessTools.Field(typeof(SaberSwingRatingCounter), "_beforeCutRating");
	        var fieldAfterCutRating = AccessTools.Field(typeof(SaberSwingRatingCounter), "_afterCutRating");
	        var fieldRateBeforeCut = (object)AccessTools.Field(typeof(SaberSwingRatingCounter), "_rateBeforeCut");
	        var fieldRateAfterCut = (object)AccessTools.Field(typeof(SaberSwingRatingCounter), "_rateAfterCut");
	        
	        #region Correct Ratings
	        
	        var beforeRating = il.DeclareLocal(typeof(float)).LocalIndex;
	        var afterRating = il.DeclareLocal(typeof(float)).LocalIndex;
	        var ifRateAfterLabel = il.DefineLabel();
	        var skipAfterCutCorrectionLabel = il.DefineLabel();
	        
	        res[193].labels.Add(skipAfterCutCorrectionLabel);
	        res.InsertRange(193, new [] {
		        // Store before rating
		        new CodeInstruction(OpCodes.Ldarg_0),
		        new CodeInstruction(OpCodes.Ldfld, fieldBeforeCutRating),
		        new CodeInstruction(OpCodes.Stloc_S, beforeRating),
		        
		        // Store after rating
		        new CodeInstruction(OpCodes.Ldarg_0),
		        new CodeInstruction(OpCodes.Ldfld, fieldAfterCutRating),
		        new CodeInstruction(OpCodes.Stloc_S, afterRating),
		        
		        // if _rateBeforeCut == true, skip before cut correction
		        new CodeInstruction(OpCodes.Ldarg_0),
		        new CodeInstruction(OpCodes.Ldfld, fieldRateBeforeCut),
		        new CodeInstruction(OpCodes.Brtrue_S, ifRateAfterLabel),
		        
		        // set _beforeCutRating to 1f
		        new CodeInstruction(OpCodes.Ldarg_0),
		        new CodeInstruction(OpCodes.Ldc_R4, 1f),
		        new CodeInstruction(OpCodes.Stfld, fieldBeforeCutRating),
		        
		        // if _rateAfterCut == true, skip after cut correction
		        new CodeInstruction(OpCodes.Ldarg_0).AddLabel(ifRateAfterLabel),
		        new CodeInstruction(OpCodes.Ldfld, fieldRateAfterCut),
		        new CodeInstruction(OpCodes.Brtrue_S, skipAfterCutCorrectionLabel),
		        
		        // set _afterCutRating to 1f
		        new CodeInstruction(OpCodes.Ldarg_0),
		        new CodeInstruction(OpCodes.Ldc_R4, 1f),
		        new CodeInstruction(OpCodes.Stfld, fieldAfterCutRating)
	        });
	        
	        res.InsertRange(res.Count, new [] {
		        // restore before rating
		        new CodeInstruction(OpCodes.Ldarg_0),
		        new CodeInstruction(OpCodes.Ldloc_S, beforeRating),
		        new CodeInstruction(OpCodes.Stfld, fieldBeforeCutRating),
		        
		        // restore after rating
		        new CodeInstruction(OpCodes.Ldarg_0),
		        new CodeInstruction(OpCodes.Ldloc_S, afterRating),
		        new CodeInstruction(OpCodes.Stfld, fieldAfterCutRating)
	        });
	        
	        #endregion

	        #region Remove If Statements

	        // remove if statements which check _rateBeforeCut and _rateAfterCut
	        for (var i = 0; i < res.Count - 2; i++) {
		        var ci = res[i];
		        var next = res[i + 1];

		        if (ci.opcode != OpCodes.Ldarg_0
		            || next.opcode != OpCodes.Ldfld
		            || (next.operand != fieldRateBeforeCut && next.operand != fieldRateAfterCut)
		            || (res[i + 2].opcode != OpCodes.Brfalse && res[i + 2].opcode != OpCodes.Brfalse_S)) continue;

		        res[i + 3].MoveLabelsFrom(res[i]);
		        res.RemoveRange(i, 3);
	        }
	        
	        #endregion
	        
	        return res;
        }

        [HarmonyPatch(nameof(SaberSwingRatingCounter.Finish))]
        [HarmonyPrefix]
        static bool Finish_Prefix(SaberSwingRatingCounter __instance,
	        bool ____rateBeforeCut,
	        bool ____rateAfterCut,
	        ref float ____beforeCutRating,
	        ref float ____afterCutRating) {
	        if (!____rateBeforeCut)
		        ____beforeCutRating = 1f;

	        if (!____rateAfterCut)
		        ____afterCutRating = 1f;

	        return true;
        }

        static CodeInstruction AddLabel(this CodeInstruction instruction, Label label) {
	        instruction.labels.Add(label);
	        return instruction;
        }
    }
}