﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.extensions;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace DigitalStorageUnit._harmony;

[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[HarmonyPatch(typeof(Pawn_JobTracker))]
public class Patch_Pawn_JobTracker
{
    /// <summary>
    ///     Patch for the AccessPointPortBuilding
    ///     Pawns starting Jobs check the IO Port for Items
    ///     This affects mostly Bills on Workbenches
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(Pawn_JobTracker.StartJob))]
    public static bool StartJob(Job newJob,
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
        if (___pawn?.Faction is not { IsPlayer: true }) return true;
        // PickUpAndHaul "Compatibility" (by not messing with it)
        if (newJob.def.defName == "HaulToInventory") return true;

        // Determines this is haul job (there is TargetA item) or not (otherwise this is possibly bill job)
        var isHaulJobType = newJob.targetA.Thing?.def?.EverStorable(false) ?? false;

        // This is the Position where we need the Item to be at
        IntVec3 destinationPos;
        if (isHaulJobType)
        {
            // Haul Type Job
            destinationPos = newJob.targetB.Thing?.Position ?? newJob.targetB.Cell;
            if (destinationPos == IntVec3.Invalid) destinationPos = ___pawn.Position;
            if (newJob.targetA == null) return true; // as is
        }
        else
        {
            // Bill Type Job
            destinationPos = newJob.targetA.Thing?.Position ?? newJob.targetA.Cell;
            if (newJob.targetB == IntVec3.Invalid && (newJob.targetQueueB == null || newJob.targetQueueB.Count == 0)) return true; // as is
        }

        List<LocalTargetInfo> targetItems;
        if (isHaulJobType)
        {
            // Haul Type Job
            targetItems = [newJob.targetA];
        }
        else
        {
            // Bill Type Job
            if (newJob.targetQueueB == null || newJob.targetQueueB.Count == 0)
                targetItems = [newJob.targetB];
            else
                targetItems = newJob.targetQueueB;
        }

        var component = ___pawn.Map.GetDsuComponent();
        if (component is null) return true; // as is

        // So, go over every job's targets. For a bill job - there is all the ingredients.
        foreach (var target in targetItems)
        {
            if (!target.HasThing) continue; // Do nothig, let the game do it's things.

            // Dirty contracted hack o-0, where we do a dirty things. See summary.
            var result = Patch_Reachability.CanReachAndFindAccessPoint(___pawn, target.Thing, destinationPos, PathEndMode.Touch, TraverseParms.For(___pawn));

            // If there are no DSU/point - let the game alone.
            if (result.Dsu is null || result.AccessPoint is null) continue; // Do nothing, let the game do its things.

            // Add to queue and try to push item to the access point
            result.AccessPoint.ProvideItem(target.Thing);
        }

        return true;
    }
}