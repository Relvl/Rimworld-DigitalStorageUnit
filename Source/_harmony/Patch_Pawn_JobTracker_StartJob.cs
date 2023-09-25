using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.map;
using DigitalStorageUnit.util;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace DigitalStorageUnit._harmony;

/// <summary>
/// Patch for the Building_AdvancedStorageUnitIOPort
/// Pawns starting Jobs check the IO Port for Items
/// This affects mostly Bills on Workbenches
/// </summary>
[HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class Patch_Pawn_JobTracker_StartJob
{
    public static bool Prefix(Job newJob,
        ref Pawn ___pawn,
        JobCondition lastJobEndCondition = JobCondition.None,
        ThinkNode jobGiver = null,
        bool resumeCurJobAfterwards = false,
        bool cancelBusyStances = true,
        ThinkTreeDef thinkTree = null,
        JobTag? tag = null,
        bool fromQueue = false,
        bool canReturnCurJobToPool = false)
    {
        // No random moths eating my cloths
        if (___pawn?.Faction == null || !___pawn.Faction.IsPlayer) return true;
        // PickUpAndHaul "Compatibility" (by not messing with it)
        if (newJob.def.defName == "HaulToInventory") return true;

        // This is the Position where we need the Item to be at
        IntVec3 targetPos;
        var isHaulJobType = newJob.targetA.Thing?.def?.category == ThingCategory.Item;

        // GetTargetPos
        if (isHaulJobType)
        {
            // Haul Type Job
            targetPos = newJob.targetB.Thing?.Position ?? newJob.targetB.Cell;
            if (targetPos == IntVec3.Invalid) targetPos = ___pawn.Position;
            if (newJob.targetA == null) return true; // as is
        }
        else
        {
            // Bill Type Jon
            targetPos = newJob.targetA.Thing?.Position ?? newJob.targetA.Cell;
            if (newJob.targetB == IntVec3.Invalid && (newJob.targetQueueB == null || newJob.targetQueueB.Count == 0)) return true; // as is
        }

        var ports = AdvancedIOPatchHelper.GetOrderdAdvancedIOPorts(___pawn.Map, ___pawn.Position, targetPos);

        List<LocalTargetInfo> targetItems;
        if (isHaulJobType)
            targetItems = new List<LocalTargetInfo> { newJob.targetA };
        else
        {
            if (newJob.targetQueueB == null || newJob.targetQueueB.Count == 0)
                targetItems = new List<LocalTargetInfo> { newJob.targetB };
            else
                targetItems = newJob.targetQueueB;
        }

        foreach (var target in targetItems)
        {
            if (target.Thing == null) continue;

            var distanceToTarget = AdvancedIOPatchHelper.GetTotalDistance(___pawn.Position, target.Cell, targetPos);

            // Quick check if the Item could be in a DSU
            // Might have false Positives They are then filterd by AdvancedIO_PatchHelper.CanMoveItem
            // But should not have false Negatives
            if (!___pawn.Map.GetDsuComponent().HideItems.Contains(target.Cell)) continue;

            foreach (var (distance, port) in ports)
            {
                if (distance < distanceToTarget ||
                    ( /*Patch_Reachability_CanReach.Status -- todo check the config instead! &&*/
                        ___pawn.Map.reachability.CanReach(___pawn.Position, target.Thing, PathEndMode.Touch, TraverseParms.For(___pawn)) &&
                        Patch_Reachability_CanReach.CanReachThing(target.Thing)))
                {
                    if (!AdvancedIOPatchHelper.CanMoveItem(port, target.Cell)) continue;
                    port.AddItemToQueue(target.Thing);
                    port.updateQueue();
                }

                // Since we use a orderd List we know if one ins further, the same is true for the rest
                break;
            }
        }

        return true;
    }
}