using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.Common.HarmonyPatches;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DigitalStorageUnit.HarmonyPatches;

/// <summary>
/// Disables the item acceptance if DSU is overfilled or forbidden
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
[HarmonyPatch(typeof(Building_Storage), nameof(Building_Storage.Accepts), typeof(Thing))]
class Patch_Building_Storage_Accepts
{
    static bool Prefix(Building_Storage __instance, Thing t, out bool __result)
    {
        __result = false;

        if (__instance is IForbidPawnInputItem forbid)
        {
            if (forbid.ForbidPawnInput)
            {
                return false; // skip the original and next prefixes
            }
        }

        return true; // continue the original
    }
}