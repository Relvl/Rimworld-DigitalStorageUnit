using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.extensions;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DigitalStorageUnit._harmony;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
[HarmonyPatch(typeof(MinifiedThing))]
public class Patch_MinifiedThing
{
    /// <summary>
    ///     Hides things below the DigitalStorageUnitBuilding
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(MinifiedThing.Print), typeof(SectionLayer))]
    static bool Prefix(Thing __instance)
    {
        return !__instance.def.EverStorable(false) || !__instance.Map.IsDSUOnPoint(__instance.Position);
    }
}