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
/// This Transpiler prevents Vanilla 1.4 from generating Gizmos for each item contained in a Building_Storage
/// This prevents UI Clutter and also improves performance compared to vanilla 1.4 without this Patch
/// </summary>
[HarmonyPatch]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal class Patch_Building_Storage_GetGizmos
{
    /// <summary>
    /// The target method is found using the custom logic defined here
    /// </summary>
    /// <returns>GetGizmos iterator</returns>
    public static MethodBase TargetMethod()
    {
        var predicateClass = typeof(Building_Storage).GetNestedTypes(AccessTools.all).FirstOrDefault(t => t.FullName!.Contains("d__52"));
        if (predicateClass == null)
        {
            Log.Error("DSU Harmony Error - predicateClass == null for Patch_Building_Storage_GetGizmos.TargetMethod()");
            return null;
        }

        var methodInfo = predicateClass.GetMethods(AccessTools.all).FirstOrDefault(t => t.Name.Contains("MoveNext"));
        if (methodInfo == null)
        {
            Log.Error("DSU Harmony Error - methodInfo == null for Patch_Building_Storage_GetGizmos.TargetMethod()");
        }

        return methodInfo;
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var Found_get_NumSelected = false;
        object endJumpMarker = null;
        var addedJump = false;
        foreach (var instruction in instructions)
        {
            //Used for refrence of the pos withing the IL
            if (instruction.opcode == OpCodes.Callvirt && instruction.operand.ToString().Contains("get_NumSelected()"))
            {
                Found_get_NumSelected = true;
            }

            //Get the Jumpmarker for the End
            if (Found_get_NumSelected && endJumpMarker == null && instruction.opcode == OpCodes.Bne_Un)
            {
                endJumpMarker = instruction.operand;
            }

            if (!addedJump && Found_get_NumSelected && instruction.opcode == OpCodes.Ldarg_0)
            {
                //Check if this is a DSU Storage Building
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldloc_2);
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_Building_Storage_GetGizmos), nameof(IsDsuBuilding)));
                //Skip to the End if yes
                yield return new CodeInstruction(OpCodes.Brtrue_S, endJumpMarker);
                addedJump = true;
            }

            //Keep the rest
            yield return instruction;
        }
    }

    public static bool IsDsuBuilding(Building_Storage building) => building is DigitalStorageUnitBuilding;
}