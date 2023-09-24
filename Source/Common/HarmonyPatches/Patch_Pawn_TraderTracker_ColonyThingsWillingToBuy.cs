using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace DigitalStorageUnit.Common.HarmonyPatches;

[HarmonyPatch(typeof(Pawn_TraderTracker), nameof(Pawn_TraderTracker.ColonyThingsWillingToBuy), typeof(Pawn))]
class Patch_Pawn_TraderTracker_ColonyThingsWillingToBuy
{
    static void Postfix(Pawn playerNegotiator, ref IEnumerable<Thing> __result)
    {
        var map = playerNegotiator.Map;
        if (map is null) return;

        var yieldedThings = new HashSet<Thing>();
        yieldedThings.AddRange(__result);
        foreach (var dsu in TradePatchHelper.AllPowered(map))
        {
            //Only for Cold Storage
            if (dsu.AdvancedIOAllowed) continue;

            yieldedThings.AddRange(dsu.StoredItems);
        }

        __result = yieldedThings;
    }
}