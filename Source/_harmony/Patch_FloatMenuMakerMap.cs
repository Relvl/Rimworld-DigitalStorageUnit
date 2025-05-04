using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.extensions;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace DigitalStorageUnit._harmony;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
[HarmonyPatch(typeof(FloatMenuMakerMap))]
public static class Patch_FloatMenuMakerMap
{
    /// <summary>
    ///     Disables right clicks on items below the DigitalStorageUnitBuilding
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(FloatMenuMakerMap.ChoicesAtFor), typeof(Vector3), typeof(Pawn), typeof(bool))]
    static bool ChoicesAtFor(Vector3 clickPos, Pawn pawn, out List<FloatMenuOption> __result)
    {
        if (pawn.IsDSUOnPoint(clickPos.ToIntVec3()))
        {
            __result = [];
            return false;
        }

        __result = null;
        return true;
    }
}