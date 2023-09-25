using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.util;
using HarmonyLib;
using Verse;

namespace DigitalStorageUnit._harmony;

/// <summary>
/// Notify the DSU when held item is changing position
/// </summary>
[HarmonyPatch(typeof(Thing), "set_Position")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class Patch_Thing_SetPosition
{
    public static void Prefix(Thing __instance, out DigitalStorageUnitBuilding __state)
    {
        __state = null;
        if (__instance.def.category == ThingCategory.Item && __instance.Position.IsValid && __instance.Map != null)
        {
            __state = __instance.Position.GetFirst<DigitalStorageUnitBuilding>(__instance.Map);
        }
    }

    public static void Postfix(Thing __instance, DigitalStorageUnitBuilding __state)
    {
        __state?.Notify_LostThing(__instance);
    }
}