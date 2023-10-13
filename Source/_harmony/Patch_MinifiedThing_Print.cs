using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.util;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DigitalStorageUnit._harmony;

/// <summary>
/// Hides things below the DigitalStorageUnitBuilding
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
        if (__instance.def.EverStorable(false) && (__instance?.Map?.GetDsuComponent()?.DsuOccupiedPoints.ContainsKey(__instance.Position) ?? false))
        {
            return false; // skip the original and next prefixes
        }

        return true; // continue the original
    }
}