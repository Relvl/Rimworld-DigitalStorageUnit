using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.util;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace DigitalStorageUnit._harmony;

[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "UnusedParameter.Global")]
[HarmonyPatch(typeof(ReservationManager))]
public static class Patch_Reservation
{
    
    /// <summary>
    /// This patch simulates reservation for ports.
    /// TODO! Looks broken, at least with LWM's DS
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(ReservationManager.Reserve))]
    public static bool Reserve(Pawn claimant, Job job, LocalTargetInfo target, ref bool __result, Map ___map)
    {
        if (target.HasThing || ___map == null || !target.Cell.InBounds(___map)) return true;
        var port = target.Cell.GetThingList(___map).FirstOrDefault(t => t is ABasePortDsuBuilding);
        
        if (port is ABasePortDsuBuilding { IOMode: StorageIOMode.Input }) // Todo! Replace with AlwaysCanReserveComp OR ModExtension
        {
            __result = true; // Always return "yes can reserve" for input ports
            return false; // stop the method
        }

        return true;
    }
}