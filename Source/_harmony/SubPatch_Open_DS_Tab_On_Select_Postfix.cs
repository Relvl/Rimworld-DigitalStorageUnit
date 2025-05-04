using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DigitalStorageUnit.util;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DigitalStorageUnit._harmony;

/// <summary>
///     YES. This is patch that patching another patch. Sorry. =(
///     Actually prevents DS to throw an errors about empty enumeration.
///     No HarmonyPatch - we use it conditionally, see usages
/// </summary>
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public static class SubPatch_Open_DS_Tab_On_Select_Postfix
{
    [HarmonyPrefix]
    public static bool Prefix()
    {
        var thing = Find.Selector.SingleSelectedThing;
        if (thing is not ThingWithComps) return true; // continue

        if (!thing.def.thingClass.GetInterfaces().Contains(typeof(ILwmDsLeaveMeAlonePlease))) return true; // contimue

        if (MainButtonDefOf.Inspect.TabWindow is MainTabWindow_Inspect { OpenTabType: not null } tabWindowInspect)
            if (thing.GetInspectTabs().Any(t => t.GetType() == tabWindowInspect.OpenTabType))
                return true; // contimue

        if (thing.def.thingClass == typeof(DigitalStorageUnitBuilding))
        {
            // todo! open itab

            // if there any items stored
            // try get storage tab
            // try get my itab
            // open it
        }

        return false; // stop right there!
    }
}