using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DigitalStorageUnit._harmony;

/// <summary>
///     Removes a content enumeration in the detailed description in the inspection pane for DSU.
/// </summary>
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[HarmonyPatch(typeof(Building_Storage))]
public static class Patch_Building_Storage
{
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(Building_Storage.GetInspectString))]
    public static IEnumerable<CodeInstruction> GetInspectString(IEnumerable<CodeInstruction> instructions)
    {
        var brFalseCount = 0;
        var patched = false;
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Brfalse) brFalseCount++; // todo! need more fuzzy search
            if (brFalseCount == 2 && !patched)
            {
                yield return instruction;

                yield return new CodeInstruction(OpCodes.Ldarg_0); // this
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_Building_Storage), nameof(IsDsuBuilding)));
                yield return new CodeInstruction(OpCodes.Brtrue_S, instruction.operand); // Skip the enumeration if call returns TRUE

                patched = true;
                Log.Message("DSU: Patch_Buildin_Storage_GetInspectString OK");
                continue;
            }

            yield return instruction;
        }
    }

    private static bool IsDsuBuilding(Building_Storage building)
    {
        return building is DigitalStorageUnitBuilding;
    }

    [HarmonyPatch(typeof(Building_Storage))]
    public static class InnerPatch_GetGizmos
    {
        /// <summary>
        ///     The target method is found using the custom logic defined here
        /// </summary>
        /// <returns>GetGizmos iterator</returns>
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            var predicateClass = typeof(Building_Storage).GetNestedTypes(AccessTools.all).FirstOrDefault(t => t.FullName!.Contains("d__52"));
            if (predicateClass == null)
            {
                Log.Error("DSU Harmony Error - predicateClass == null for Patch_Building_Storage_GetGizmos.TargetMethod()");
                return null;
            }

            var methodInfo = predicateClass.GetMethods(AccessTools.all).FirstOrDefault(t => t.Name.Contains("MoveNext"));
            if (methodInfo == null) Log.Error("DSU Harmony Error - methodInfo == null for Patch_Building_Storage_GetGizmos.TargetMethod()");

            return methodInfo;
        }

        /// <summary>
        ///     This Transpiler prevents Vanilla 1.4 from generating Gizmos for each item contained in a Building_Storage
        ///     This prevents UI Clutter and also improves performance compared to vanilla 1.4 without this Patch
        /// </summary>
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var Found_get_NumSelected = false;
            object endJumpMarker = null;
            var patched = false;
            foreach (var instruction in instructions)
            {
                //Used for refrence of the pos withing the IL
                if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("get_NumSelected()")) Found_get_NumSelected = true;

                //Get the Jumpmarker for the End
                if (Found_get_NumSelected && endJumpMarker == null && instruction.opcode == OpCodes.Bne_Un) endJumpMarker = instruction.operand;

                if (!patched && Found_get_NumSelected && instruction.opcode == OpCodes.Ldarg_0)
                {
                    //Check if this is a DSU Storage Building
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloc_2);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_Building_Storage), nameof(IsDsuBuilding)));
                    //Skip to the End if yes
                    yield return new CodeInstruction(OpCodes.Brtrue_S, endJumpMarker);
                    patched = true;
                    Log.Message("DSU: Patch_Building_Storage_GetGizmos OK");
                }

                //Keep the rest
                yield return instruction;
            }
        }
    }
}