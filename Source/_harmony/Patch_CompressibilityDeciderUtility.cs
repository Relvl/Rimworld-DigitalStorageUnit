using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.util;
using HarmonyLib;
using Verse;

namespace DigitalStorageUnit._harmony;

[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[HarmonyPatch(typeof(CompressibilityDeciderUtility))]
public static class Patch_CompressibilityDeciderUtility
{
    /// <summary>
    ///     TODO! What this does? What is Def/ThingDef/saveCompressible?
    ///     Blocks using "compression" (?) to the items inside DSU
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(CompressibilityDeciderUtility.IsSaveCompressible))]
    public static bool IsSaveCompressible(Thing t, ref bool __result)
    {
        if (t.Map is null) return true;
        if (!t.Position.IsValid) return true;
        if (t.Position.GetFirst<DigitalStorageUnitBuilding>(t.Map) == null) return true;
        __result = false;
        return false;
    }
}