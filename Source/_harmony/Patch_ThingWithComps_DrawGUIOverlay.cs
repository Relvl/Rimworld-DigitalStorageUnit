using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.util;
using HarmonyLib;
using Verse;

namespace DigitalStorageUnit._harmony;

/// <summary>
/// Hides things' labels (etc) below the DigitalStorageUnitBuilding
/// </summary>
[HarmonyPatch(typeof(ThingWithComps), nameof(ThingWithComps.DrawGUIOverlay))]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class Patch_ThingWithComps_DrawGUIOverlay
{
    public static bool Prefix(Thing __instance)
    {
        if (__instance.def.EverStorable(false) && (__instance.Map?.GetDsuComponent()?.DsuOccupiedPoints.ContainsKey(__instance.Position) ?? false))
        {
            return false; // skip the original and next prefixes
        }

        return true; // continue the original
    }
}