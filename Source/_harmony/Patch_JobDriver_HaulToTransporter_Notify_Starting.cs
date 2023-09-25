using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.util;
using HarmonyLib;
using RimWorld;

namespace DigitalStorageUnit._harmony;

/// <summary>
/// Patch lets haul to transporter job gives items from port instead DSU if dispance(!) is shortest.
/// </summary>
[HarmonyPatch(typeof(JobDriver_HaulToTransporter), "Notify_Starting")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class Patch_JobDriver_HaulToTransporter_Notify_Starting
{
    public static void Postfix(JobDriver_HaulToTransporter __instance)
    {
        var thingPos = __instance.job.targetA.Cell;
        var transporterPos = __instance.job.targetB.Cell;
        // todo! needs some checks if the DSU holds this thing
        var thingDist = AdvancedIOPatchHelper.GetTotalDistance(__instance.pawn.Position, thingPos, transporterPos);
        var closest = AdvancedIOPatchHelper.GetClosestPort(__instance.pawn.Map, __instance.pawn.Position, transporterPos, __instance.job.targetA.Thing, thingDist);
        var closestPort = closest.Value;
        if (closestPort is null) return;
        __instance.job.targetA.Thing.Position = closestPort.Position;
    }
}