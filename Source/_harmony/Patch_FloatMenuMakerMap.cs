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
    [HarmonyPatch(nameof(FloatMenuMakerMap.GetOptions))]
    static bool GetOptions(Vector3 clickPos, ref List<FloatMenuOption> __result, out FloatMenuContext context)
    {
        if (Find.CurrentMap.IsDSUOnPoint(clickPos.ToIntVec3()))
        {
            __result = [];
            context = null;
            return false;
        }

        __result = null;
        context = null;
        return true;
    }
    
}