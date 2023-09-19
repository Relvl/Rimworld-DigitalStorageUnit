using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.Common;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace DigitalStorageUnit.HarmonyPatches;

/// <summary>
/// Disables right clicks on items below the Building_MassStorageUnit
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
        if (pawn.Map.GetDsuComponent().HideRightMenus.Contains(clickPos.ToIntVec3()))
        {
            __result = new List<FloatMenuOption>();
            return false;
        }

        __result = null;
        return true;
    }
}