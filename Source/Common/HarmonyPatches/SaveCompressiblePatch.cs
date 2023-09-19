using DigitalStorageUnit.Storage;
using HarmonyLib;
using Verse;

namespace DigitalStorageUnit.Common.HarmonyPatches;

[HarmonyPatch(typeof(CompressibilityDeciderUtility), "IsSaveCompressible")]
public static class SaveCompressiblePatch
{
    public static void Postfix(Thing t, ref bool __result)
    {
        if (__result && t.Map != null && t.Position.IsValid && t.Position.GetFirst<Building_MassStorageUnit>(t.Map) != null)
        {
            __result = false;
        }
    }
}