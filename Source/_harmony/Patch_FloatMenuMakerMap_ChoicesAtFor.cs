using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.util;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace DigitalStorageUnit._harmony;

/// <summary>
/// Disables right clicks on items below the DigitalStorageUnitBuilding
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
[HarmonyPatch(typeof(FloatMenuMakerMap))]
[HarmonyPatch(nameof(FloatMenuMakerMap.ChoicesAtFor), typeof(Vector3), typeof(Pawn), typeof(bool))]
class Patch_FloatMenuMakerMap_ChoicesAtFor
{
    static bool Prefix(Vector3 clickPos, Pawn pawn, out List<FloatMenuOption> __result)
    {
        if (pawn?.Map?.GetDsuComponent()?.DsuOccupiedPoints.ContainsKey(clickPos.ToIntVec3()) ?? false)
        {
            __result = new List<FloatMenuOption>();
            return false;
        }

        __result = null;
        return true;
    }
}