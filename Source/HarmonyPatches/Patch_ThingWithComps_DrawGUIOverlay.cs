using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.Common;
using HarmonyLib;
using Verse;

namespace DigitalStorageUnit.HarmonyPatches;

/// <summary>
/// Hides things' labels (etc) below the Building_MassStorageUnit
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
[HarmonyPatch(typeof(ThingWithComps))]
[HarmonyPatch(nameof(ThingWithComps.DrawGUIOverlay))]
class Patch_ThingWithComps_DrawGUIOverlay
{
    static bool Prefix(Thing __instance)
    {
        if (__instance.def.category == ThingCategory.Item)
        {
            if (__instance.Map.GetDsuComponent()?.HideItems.Contains(__instance.Position) ?? false)
            {
                return false; // skip the original and next prefixes
            }
        }

        return true; // continue the original
    }
}