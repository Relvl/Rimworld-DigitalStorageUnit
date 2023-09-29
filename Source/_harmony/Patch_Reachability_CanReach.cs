using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DigitalStorageUnit.map;
using DigitalStorageUnit.util;
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
    [Obsolete] private static Thing LastCanReachThing;

    /// <summary>
    /// Holds the Last item that was checked and Required the use of a Advanced IO Port
    /// This is used in other patches to force the use of an IO Port
    /// TODO! Looks like a crutch actually =(
    /// </summary>
    [Obsolete]
    public static bool CanReachThingViaAccessPonit(Thing thing)
    {
        var ret = thing == LastCanReachThing;
        LastCanReachThing = null;
        return ret;
    }

    /// <summary>
    /// Oof... Dirty thing, we need to determine best access point here, but we can't do it without job destination position.
    /// </summary>
    private static IntVec3 _jobDestination = IntVec3.Invalid;

    /// <summary>
    /// The pawn that initiate this job
    /// </summary>
    private static Pawn _jobPawn;

    /// <summary>
    /// The DSU that we checking for.
    /// </summary>
    private static DigitalStorageUnitBuilding _cachedDsu;

    /// <summary>
    /// Best Access Point that we found here.
    /// </summary>
    private static Building_AdvancedStorageUnitIOPort _cachedAccessPoint;

    /// <summary>
    /// Dirty hack to return extra info from the patched method.
    /// We have to make a contract: we must set up job destination first, then call Reachability.CanReach, and then get cached data thru this method; WITHOUT any middle actions.
    /// I thing something may went wrong only if multithreading.
    /// </summary>
    /// <returns>Method result, found DSU, and better Access Point to move item.</returns>
    public static (DigitalStorageUnitBuilding, Building_AdvancedStorageUnitIOPort) CanReachAndFindAccessPoint(Pawn pawn, LocalTargetInfo middlePoint, IntVec3 jobDestination)
    {
        _jobDestination = jobDestination;
        _jobPawn = pawn;

        pawn.Map.reachability.CanReach(pawn.Position, middlePoint, PathEndMode.Touch, TraverseParms.For(pawn));
        try
        {
            return (_cachedDsu, _cachedAccessPoint);
        }
        finally
        {
            _cachedDsu = null;
            _cachedAccessPoint = null;
            _jobPawn = null;
            _jobDestination = IntVec3.Invalid;
        }
    }

    public static void Postfix(IntVec3 start, LocalTargetInfo dest, PathEndMode peMode, TraverseParms traverseParams, ref bool __result, Map ___map, Reachability __instance)
    {
        // Cannot be done without destination request
        if (_jobDestination == IntVec3.Invalid) return; // as is
        // We can't continue without the pawn
        if (_jobPawn is null) return; // as is
        //Ignore everything that is not a Item
        if (dest.Thing?.def.category != ThingCategory.Item) return; // as is

        var component = ___map.GetDsuComponent();
        var dsu = component?.DsuOccupiedPoints.TryGetValue(dest.Thing.Position);
        if (dsu is null || !dsu.Powered /* todo! if forbidden for pawn access directly */) return; // as is

        // Initial best distance - is Distance(pawn -> item -> destionation) multiplied on "avoid DSU factor" if needed
        var bestDistance = __result ? component.GetTotalDistance(_jobPawn, dest.Cell, _jobDestination, true) : float.MaxValue;
        Building_AdvancedStorageUnitIOPort bestAccessPoint = null;
        foreach (var accessPoint in component.AccessPointSet)
        {
            // Bound DSU is not the same - skip
            if (accessPoint.boundStorageUnit != dsu) continue;
            // Access Point is occupied by another item // todo! hmmmm... so pawn will go to another, or DSU. not good actually.
            if (!accessPoint.CanReceiveNewItem) continue;
            // Distance (pawn -> access point -> job destination)
            var distance = component.GetTotalDistance(_jobPawn, accessPoint.Position, _jobDestination);
            // If distance to DSU/previous is shorter - skip
            if (bestDistance < distance) continue;
            // We dont care about recursion - now we check not an item.
            // If the pawn can't reach the Access Point - skip
            if (!__instance.CanReach(start, accessPoint.Position, PathEndMode.Touch, traverseParams)) continue;

            bestDistance = distance;
            bestAccessPoint = accessPoint;

            // TODO! OBSOLETE
            LastCanReachThing = dest.Thing;
        }

        _cachedDsu = dsu;
        _cachedAccessPoint = bestAccessPoint;
        __result = __result || _cachedAccessPoint is not null;
    }
}