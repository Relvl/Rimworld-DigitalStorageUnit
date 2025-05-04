using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.extensions;
using DigitalStorageUnit.util;
using HarmonyLib;
using Verse;

namespace DigitalStorageUnit._harmony;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[HarmonyPatch(typeof(Thing))]
public static class Patch_Thing
{
    /// <summary>
    ///     Hides things' labels (etc) below the DigitalStorageUnitBuilding
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Thing.DrawGUIOverlay))]
    public static bool DrawGUIOverlay(Thing __instance)
    {
        if (!__instance.def.EverStorable(false)) return true;
        return !__instance.Map.IsDSUOnPoint(__instance.Position);
    }

    /// <summary>
    ///     Hides things below the DigitalStorageUnitBuilding
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Thing.Print), typeof(SectionLayer))]
    public static bool Print(Thing __instance)
    {
        if (!__instance.def.EverStorable(false)) return true;
        return !__instance.Map.IsDSUOnPoint(__instance.Position);
    }

    /// <summary>
    ///     Notify the DSU when held item is changing position
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch("set_Position")]
    public static void Prefix(Thing __instance, out IntVec3 __state)
    {
        __state = __instance.Position;
    }

    /// <summary>
    ///     Notify the DSU when held item is changing position
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch("set_Position")]
    public static void Postfix(Thing __instance, IntVec3 __state)
    {
        if (__instance.Map is null) return;
        if (__instance.Position == __state) return;
        if (!__instance.def.EverStorable(false)) return;

        var oldDsu = __state.IsValid ? __state.GetFirst<DigitalStorageUnitBuilding>(__instance.Map) : null;
        // todo check PRF commit ed2e813fb92ad032eb08160255e3e4e6a274fb7a
        var newDsu = __instance.Position.IsValid ? __instance.Position.GetFirst<DigitalStorageUnitBuilding>(__instance.Map) : null;
        if (newDsu == oldDsu) return;
        newDsu?.Notify_ReceivedThing(__instance);
        oldDsu?.Notify_LostThing(__instance);
    }
}