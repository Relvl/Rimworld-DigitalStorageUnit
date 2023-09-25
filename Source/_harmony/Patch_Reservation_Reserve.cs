using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.util;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace DigitalStorageUnit._harmony;

/// <summary>
/// This patch simulates reservation for ports.
/// TODO! Looks broken, at least with LWM's DS
/// </summary>
[HarmonyPatch(typeof(ReservationManager), nameof(ReservationManager.Reserve))]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "UnusedParameter.Global")]
public class Patch_Reservation_Reserve
{
    public static bool Prefix(Pawn claimant, Job job, LocalTargetInfo target, ref bool __result, Map ___map)
    {
        if (target.HasThing || ___map == null || !target.Cell.InBounds(___map)) return true;
        var dsu = target.Cell.GetThingList(___map).FirstOrDefault(t => t is Building_StorageUnitIOBase);
        if (dsu is Building_StorageUnitIOBase { ioMode: StorageIOMode.Input })
        {
            __result = true;
            return false; // stop the method
        }

        return true;
    }
}