using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.util;
using HarmonyLib;
using Verse;

namespace DigitalStorageUnit._harmony;

/// <summary>
/// TODO! What this does? What is Def/ThingDef/saveCompressible?
/// </summary>
[HarmonyPatch(typeof(CompressibilityDeciderUtility), nameof(CompressibilityDeciderUtility.IsSaveCompressible))]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public static class Patch_CompressibilityDeciderUtility_IsSaveCompressible
{
    // TODO! Looks like we'll be better to use Prefix, this function is clean
    public static void Postfix(Thing t, ref bool __result)
    {
        if (__result && t.Map != null && t.Position.IsValid && t.Position.GetFirst<DigitalStorageUnitBuilding>(t.Map) != null)
        {
            __result = false;
        }
    }
}