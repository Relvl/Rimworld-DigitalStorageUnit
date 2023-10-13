using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.util;
using HarmonyLib;
using Verse;

namespace DigitalStorageUnit._harmony;

/// <summary>
/// Hides things below the DigitalStorageUnitBuilding
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
[HarmonyPatch(typeof(Thing))]
[HarmonyPatch(nameof(Thing.Print), typeof(SectionLayer))]
class Patch_Thing_Print
{
    static bool Prefix(Thing __instance, SectionLayer layer)
    {
        if (__instance.def.EverStorable(false) && (__instance.Map?.GetDsuComponent()?.DsuOccupiedPoints.ContainsKey(__instance.Position) ?? false))
        {
            return false; // skip the original and next prefixes
        }

        return true; // continue the original
    }
}