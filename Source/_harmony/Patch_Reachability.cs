using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.extensions;
using DigitalStorageUnit.util;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace DigitalStorageUnit._harmony;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "ArrangeTypeMemberModifiers")]
[HarmonyPatch(typeof(Reachability))]
public static class Patch_Reachability
{
    private static ReachabilityPatchResult _cachedResult;

    /// <summary>
    /// Dirty hack to return extra info from the patched method.
    /// We have to make a contract: we must set up job destination first, then call Reachability.CanReach, and then get cached data thru this method; WITHOUT any middle actions.
    /// I think something may went wrong only if multithreading.
    /// </summary>
    /// <returns>Method result, found DSU, and better Access Point to move item.</returns>
    public static ReachabilityPatchResult CanReachAndFindAccessPoint(
        Pawn pawn,
        LocalTargetInfo middlePoint,
        IntVec3 jobDestination,
        PathEndMode peMode,
        TraverseParms traverseParams
    )
    {
        _cachedResult = new ReachabilityPatchResult { JobDestination = jobDestination, JobPawn = pawn };
        _cachedResult.DirectDistanceToTarget = (_cachedResult.JobPawn.Position - middlePoint.Cell).LengthManhattan;
        pawn.Map.reachability.CanReach(pawn.Position, middlePoint, peMode, traverseParams);
        try
        {
            return _cachedResult;
        }
        finally
        {
            _cachedResult = null;
        }
    }

    /// <summary>
    /// This Patch allows Pawns to receive Items from an Advanced IO Port when the direct Path to the DSU(current Item Location) is Blocked
    /// This Patch has a noticeable Performance Impact and shall only be use if the Path is Blocked
    /// Causes recursive CanReach!!! Should be disabled thru the settings!
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(Reachability.CanReach), typeof(IntVec3), typeof(LocalTargetInfo), typeof(PathEndMode), typeof(TraverseParms))]
    public static void CanReach(IntVec3 start, LocalTargetInfo dest, PathEndMode peMode, TraverseParms traverseParams, ref bool __result, Map ___map, Reachability __instance)
    {
        if (_cachedResult is null) 
            return; // as is
        
        _cachedResult.OriginalCanReach = __result;
        
        // Cannot be done without destination request
        if (_cachedResult.JobDestination == IntVec3.Invalid)
            return; // as is

        // We can't continue without the pawn
        if (_cachedResult.JobPawn is null)
            return; // as is

        //Ignore everything that is not an Item
        if (!(dest.Thing?.def.EverStorable(false) ?? false))
            return; // as is

        var component = ___map.GetDsuComponent();
        var dsu = component?.GetDsuHoldingItem(dest.Thing);
        if (dsu is null || !dsu.CanWork /* todo! if forbidden for pawn access directly */)
            return; // as is

        // Initial best distance - is Distance(pawn -> item -> destionation) multiplied on "avoid DSU factor" if needed
        var bestWeight = __result ? component.GetTotalDistance(_cachedResult.JobPawn, dest.Cell, _cachedResult.JobDestination, true) : float.MaxValue;
        _cachedResult.DirectPathingWeight = bestWeight;

        AccessPointPortBuilding bestAccessPoint = null;
        foreach (var accessPoint in component.GetAccessPoints(dsu))
        {
            // Total path weight (pawn -> item -> destination), this might be simplied by the settings
            var weight = component.GetTotalDistance(_cachedResult.JobPawn, accessPoint.Position, _cachedResult.JobDestination);

            // If distance to DSU/previous is shorter - skip
            if (bestWeight < weight) continue;

            // We don't care about recursion - now we check not an item.
            // If the pawn can't reach the Access Point - skip
            if (!__instance.CanReach(start, accessPoint.Position, PathEndMode.Touch, traverseParams)) continue;

            bestWeight = weight;
            bestAccessPoint = accessPoint;
        }

        _cachedResult.Dsu = dsu;
        _cachedResult.AccessPoint = bestAccessPoint;
        _cachedResult.BestPathingWeight = bestWeight;
        _cachedResult.DirectDistanceToAccessPoint = bestAccessPoint is null ? default : (_cachedResult.JobPawn.Position - bestAccessPoint.Position).LengthManhattan;

        __result = __result || _cachedResult.AccessPoint is not null;
    }
}

public class ReachabilityPatchResult
{
    /// <summary>
    /// Oof... Dirty thing, we need to determine best access point here, but we can't do it without job destination position.
    /// </summary>
    public IntVec3 JobDestination;

    /// <summary>
    /// The pawn that initiate this job
    /// </summary>
    public Pawn JobPawn;

    /// <summary>
    /// The DSU that we checking for.
    /// </summary>
    public DigitalStorageUnitBuilding Dsu;

    /// <summary>
    /// Best Access Point that we found here.
    /// </summary>
    public AccessPointPortBuilding AccessPoint;

    /// <summary>
    /// Distance (pawn -> item -> destination)
    /// </summary>
    public float BestPathingWeight;

    /// <summary>
    /// Distance (pawn -> item -> destination) without access point
    /// </summary>
    public float DirectPathingWeight;

    /// <summary>
    /// 
    /// </summary>
    public float DirectDistanceToTarget;

    /// <summary>
    /// 
    /// </summary>
    public float DirectDistanceToAccessPoint;

    /// <summary>
    /// 
    /// </summary>
    public bool OriginalCanReach;
}