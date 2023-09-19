using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.Common.HarmonyPatches;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DigitalStorageUnit.HarmonyPatches;

/// <summary>
/// Hides things below the Building_MassStorageUnit
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
[HarmonyPatch(typeof(MinifiedThing))]
[HarmonyPatch(nameof(MinifiedThing.Print), typeof(SectionLayer))]
public class Patch_MinifiedThing_Print
{
    static bool Prefix(Thing __instance, SectionLayer layer)
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