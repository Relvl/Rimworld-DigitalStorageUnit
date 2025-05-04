using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DigitalStorageUnit._harmony;

/// <summary>
/// 
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
[HarmonyPatch(typeof(ForbidUtility))]
public static class Patch_ForbidUtility
{
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ForbidUtility.IsForbidden), typeof(Thing), typeof(Pawn))]
    static bool IsForbidden(Thing t, Pawn pawn, out bool __result)
    {
        __result = true;

        /*
        if (t is not null && t.Map is not null && t.def.EverStorable(false))
        {
            if (t.Map.GetDsuComponent()?.ForbidItems.Contains(t.Position) ?? false)
            {
                return false; // skip the original and next prefixes
            }
        }*/

        return true; // continue the original
    }
}