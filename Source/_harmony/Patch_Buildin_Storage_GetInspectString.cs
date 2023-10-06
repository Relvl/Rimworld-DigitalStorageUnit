using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;

namespace DigitalStorageUnit._harmony;

/// <summary>
/// Removes a content enumeration in the detailed description in the inspection pane for DSU.
/// </summary>
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[HarmonyPatch(typeof(Building_Storage), nameof(Building_Storage.GetInspectString))]
public static class Patch_Buildin_Storage_GetInspectString
{
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
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
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_Buildin_Storage_GetInspectString), nameof(IsDsuBuilding)));
                yield return new CodeInstruction(OpCodes.Brtrue_S, instruction.operand); // Skip the enumeration if call returns TRUE

                patched = true;
                continue;
            }

            yield return instruction;
        }
    }

    private static bool IsDsuBuilding(Building_Storage building)
    {
        return building.GetType() == typeof(DigitalStorageUnitBuilding);
    }
}