using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.util;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace DigitalStorageUnit._harmony;

/// <summary>
/// Patch lets haul to transporter job gives items from port instead DSU if path is shortest.
/// </summary>
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[HarmonyPatch(typeof(JobDriver_HaulToTransporter), nameof(JobDriver_HaulToTransporter.Notify_Starting))]
public class Patch_JobDriver_HaulToTransporter_Notify_Starting
{
    public static void Postfix(JobDriver_HaulToTransporter __instance, Job ___job)
    {
        var thingPos = __instance.job.targetA.Cell;
        var transporterPos = __instance.job.targetB.Cell;

        var component = __instance.pawn?.Map?.GetDsuComponent();
        if (component is null) return;

        var reachabilityResult = Patch_Reachability_CanReach.CanReachAndFindAccessPoint(
            __instance.pawn,
            thingPos,
            transporterPos,
            PathEndMode.ClosestTouch,
            TraverseParms.For(__instance.pawn)
        );
        
        // todo! ___job.count <- splitoff

        if (reachabilityResult.Dsu is null || reachabilityResult.AccessPoint is null) return;
        reachabilityResult.AccessPoint.ProvideItem(__instance.job.targetA.Thing);
    }
}