using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.extensions;
using HarmonyLib;
using Verse;

namespace DigitalStorageUnit._harmony;

[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[HarmonyPatch(typeof(ThingWithComps))]
public class Patch_ThingWithComps
{
    /// <summary>
    ///     Hides things' labels (etc) below the DigitalStorageUnitBuilding
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ThingWithComps.DrawGUIOverlay))]
    public static bool DrawGUIOverlay(Thing __instance)
    {
        if (!__instance.def.EverStorable(false)) return true;
        return !__instance.Map.IsDSUOnPoint(__instance.Position);
    }
}