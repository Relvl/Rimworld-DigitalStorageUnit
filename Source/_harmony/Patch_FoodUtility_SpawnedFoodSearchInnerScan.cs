using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.util;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace DigitalStorageUnit._harmony;

/// <summary>
/// Allows the pawn to take food from the Access Point.
/// Stack:
///     RimWorld.FoodUtility.SpawnedFoodSearchInnerScan
///     RimWorld.FoodUtility.BestFoodSourceOnMap_NewTemp
///     RimWorld.FoodUtility.BestFoodSourceOnMap | RimWorld.JobGiver_GetFood.TryGiveJob | RimWorld.WorkGiver_InteractAnimal.TakeFoodForAnimalInteractJob | RimWorld.WorkGiver_Tame.JobOnThing
/// </summary>
[HarmonyPatch(typeof(FoodUtility), "SpawnedFoodSearchInnerScan")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class Patch_FoodUtility_SpawnedFoodSearchInnerScan
{
    /// <summary>
    /// Looks like we need totally rewrite this method...
    /// Okay. It searches the food with shortest path and better optimality within provided list.
    /// We need to determine whats better - let the pawn take it, or provide it via Access Point.
    /// </summary>
    /// <param name="eater">The pawn that will eat</param>
    /// <param name="root">Position of hauler pawn</param>
    /// <param name="searchSet">Allowed food items</param>
    /// <param name="peMode">Always PathEndMode.ClosestTouch</param>
    /// <param name="traverseParams">Traverse params of the pawn that will haul/eat</param>
    /// <param name="maxDistance"></param>
    /// <param name="validator"></param>
    /// <param name="__result">The food</param>
    /// <returns>Continue the original method or not</returns>
    public static bool Prefix(Pawn eater,
        IntVec3 root,
        IReadOnlyList<Thing> searchSet,
        PathEndMode peMode,
        TraverseParms traverseParams,
        float maxDistance,
        Predicate<Thing> validator,
        out Thing __result)
    {
        __result = null;
        if (searchSet is null) return true;
        var haulerPawn = traverseParams.pawn ?? eater;
        var bestOptimality = float.MinValue;
        var component = haulerPawn.Map.GetDsuComponent();
        if (component is null) return true;

        // Prevent same pathfinding multiple times - eater and hauler immutible over the loop
        var pathfindingCache = new Dictionary<IntVec3, ReachabilityPatchResult>();

        foreach (var foodSource in searchSet)
        {
            if (!foodSource.Spawned) continue;
            if (validator is not null && !validator(foodSource)) continue;

            var result = pathfindingCache.ContainsKey(foodSource.Position)
                ? pathfindingCache.TryGetValue(foodSource.Position)
                : Patch_Reachability_CanReach.CanReachAndFindAccessPoint(haulerPawn, foodSource.Position, eater.Position, peMode, traverseParams);
            pathfindingCache[foodSource.Position] = result;

            // If the food inside a DSU
            if (result.Dsu is not null && result.AccessPoint is not null)
            {
                if (result.DirectDistanceToAccessPoint > maxDistance) continue;
                var currentOptimality = FoodUtility.FoodOptimality(eater, foodSource, FoodUtility.GetFinalIngestibleDef(foodSource), result.DirectDistanceToAccessPoint);
                if (currentOptimality < bestOptimality) continue;
                if (validator is not null && !validator(foodSource)) continue;
                bestOptimality = currentOptimality;
            }

            // If the food NOT inside a DSU -> part of orogonal code, same algorythm
            else
            {
                if (result.DirectDistanceToTarget > maxDistance) continue; // There where always a DirectDistanceToTarget calculated
                var currentOptimality = FoodUtility.FoodOptimality(eater, foodSource, FoodUtility.GetFinalIngestibleDef(foodSource), result.DirectDistanceToTarget);
                if (currentOptimality < bestOptimality) continue;
                if (!result.OriginalCanReach) continue;
                bestOptimality = currentOptimality;
            }

            __result = foodSource;
        }

        return false;
    }
}