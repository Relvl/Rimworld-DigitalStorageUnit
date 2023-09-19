using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.Common;
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
[HarmonyPatch(typeof(ForbidUtility), nameof(ForbidUtility.IsForbidden), typeof(Thing), typeof(Pawn))]
class Patch_ForbidUtility_IsForbidden
{
    static bool Prefix(Thing t, Pawn pawn, out bool __result)
    {
        __result = true;

        // TODO! What actually this does???
        if (t is not null && t.Map is not null && t.def.category == ThingCategory.Item)
        {
            if (t.Map.GetDsuComponent()?.ForbidItems.Contains(t.Position) ?? false)
            {
                return false; // skip the original and next prefixes
            }
        }

        return true; // continue the original
    }
}