using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.map;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace DigitalStorageUnit._harmony;

/// <summary>
/// This Patch allows Pawns to receive Items from a Advanced IO Port when the direct Path to the DSU(current Item Location) is Blocked
/// This Patch has a noticeable Performance Impact and shall only be use if the Path is Blocked
/// Causes recursive CanReach!!! Should be disabled thru the settings!
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[HarmonyPatch(typeof(Reachability), nameof(Reachability.CanReach), typeof(IntVec3), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(TraverseParms))]
public class Patch_Reachability_CanReach
{
    private static Thing canReachThing;

    /// <summary>
    /// Holds the Last item that was checked and Required the use of a Advanced IO Port
    /// This is used in other patches to force the use of an IO Port
    /// TODO! Looks like a crutch actually =(
    /// </summary>
    public static bool CanReachThing(Thing thing)
    {
        var ret = thing == canReachThing;
        canReachThing = null;
        return ret;
    }

    public static void Postfix(IntVec3 start, LocalTargetInfo dest, PathEndMode peMode, TraverseParms traverseParams, ref bool __result, Map ___map, Reachability __instance)
    {
        // todo! setting that disables this

        // There is already a Path
        if (__result) return;
        //Ignore everything that is not a Item
        if (dest.Thing?.def.category != ThingCategory.Item) return;

        var mapComp = ___map.GetDsuComponent();
        if (mapComp is null) return;

        // Quickly Check if the Item is in a Storage Unit
        // TODO: Rework that -> This includes items in PRF Crates & Excludes items from Cold Storage(Note they currently have bigger issues)
        if (!mapComp.HideItems.Contains(dest.Thing.Position)) return;

        // Check Every Advanced IO Port
        foreach (var (target, port) in mapComp.AdvancedPortLocations)
        {
            // Check if that Port has access to the Item
            // TODO: Rework that -> Is the Use of the Position really best?
            if (port.boundStorageUnit?.Position != dest.Thing.Position) continue;
            if (!__instance.CanReach(start, target, PathEndMode.Touch, traverseParams)) continue;

            canReachThing = dest.Thing;
            __result = true;
            return;
        }
    }
}