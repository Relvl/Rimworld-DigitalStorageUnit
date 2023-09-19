using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.Common.HarmonyPatches;
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
[HarmonyPatch(typeof(Thing))]
[HarmonyPatch(nameof(Thing.DrawGUIOverlay))]
class Patch_Thing_DrawGUIOverlay
{
    static bool Prefix(Thing __instance)
    {
        if (__instance.def.category == ThingCategory.Item)
        {
            if (PatchStorageUtil.GetPRFMapComponent(__instance.Map)?.ShouldHideItemsAtPos(__instance.Position) ?? false)
            {
                return false; // skip the original and next prefixes
            }
        }

        return true; // continue the original
    }
}