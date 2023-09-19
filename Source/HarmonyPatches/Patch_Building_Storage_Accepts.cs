using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.Common.HarmonyPatches;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DigitalStorageUnit.HarmonyPatches;

/// <summary>
/// 
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
        
        // TODO! What actually this does??? 
        // Check if pawn input is forbidden
        if ((__instance as IForbidPawnInputItem)?.ForbidPawnInput ?? false)
        {
            // This check is needed to support the use of the Limit function for the IO Ports
            // https://github.com/zymex22/Project-RimFactory-Revived/issues/699
            // https://github.com/zymex22/Project-RimFactory-Revived/issues/678
            if (__instance.Position != t.Position)
            {
                return false; // skip the original and next prefixes
            }
        }

        return true; // continue the original
    }
}