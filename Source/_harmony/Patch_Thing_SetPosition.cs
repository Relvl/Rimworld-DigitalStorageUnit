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
    public static void Prefix(Thing __instance, out IntVec3 __state)
    {
        __state = __instance.Position;
    }

    public static void Postfix(Thing __instance, IntVec3 __state)
    {
        if (__instance.Map is null) return;
        if (__instance.Position == __state) return;
        if (!__instance.def.EverStorable(false)) return;

        var oldDsu = __state.IsValid ? __state.GetFirst<DigitalStorageUnitBuilding>(__instance.Map) : null;
        var newDsu = __instance.Position.IsValid ? __instance.Position.GetFirst<DigitalStorageUnitBuilding>(__instance.Map) : null;
        if (newDsu == oldDsu) return;

        if (newDsu is not null) newDsu.Notify_ReceivedThing(__instance);
        if (oldDsu is not null) oldDsu.Notify_LostThing(__instance);
    }
}