using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.extensions;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace DigitalStorageUnit._harmony;

[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[HarmonyPatch(typeof(JobDriver_HaulToTransporter))]
public static class Patch_JobDriver_HaulToTransporter
{
    /// <summary>
    ///     Patch lets haul to transporter job gives items from port instead DSU if path is shortest.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(JobDriver_HaulToTransporter.Notify_Starting))]
    public static void Notify_Starting(JobDriver_HaulToTransporter __instance, Job ___job)
    {
        var thingPos = __instance.job.targetA.Cell;
        var transporterPos = __instance.job.targetB.Cell;

        var component = __instance.pawn?.Map?.GetDsuComponent();
        if (component is null) return;

        var reachabilityResult = Patch_Reachability.CanReachAndFindAccessPoint(
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