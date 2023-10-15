using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DigitalStorageUnit.util;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DigitalStorageUnit._harmony;

/// <summary>
/// YES. This is patch that patching another patch. Sorry. =(
/// Actually prevents DS to throw an errors about empty enumeration.
/// </summary>
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class Patch_Open_DS_Tab_On_Select_Postfix
{
    public static bool Prefix()
    {
        var thing = Find.Selector.SingleSelectedThing;
        if (thing is null) return true; // continue
        if (thing is not ThingWithComps) return true; // continue
        if (!thing.def.thingClass.GetInterfaces().Contains(typeof(ILwmDsLeaveMeAlonePlease))) return true; // contimue

        if (MainButtonDefOf.Inspect.TabWindow is MainTabWindow_Inspect { OpenTabType: { } } tabWindowInspect)
        {
            if (thing.GetInspectTabs().Any(t => t.GetType() == tabWindowInspect.OpenTabType)) return true; // contimue
        }

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