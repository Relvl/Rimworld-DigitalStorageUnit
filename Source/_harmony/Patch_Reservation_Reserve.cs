﻿using System.Diagnostics.CodeAnalysis;
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
        var port = target.Cell.GetThingList(___map).FirstOrDefault(t => t is ABasePortDsuBuilding);
        if (port is ABasePortDsuBuilding { IOMode: StorageIOMode.Input }) // Todo! Replace with AlwaysCanReserveComp OR ModExtension
        {
            __result = true; // Always return "yes can reserve" for input ports
            return false; // stop the method
        }

        return true;
    }
}