using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.extensions;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DigitalStorageUnit._harmony;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[HarmonyPatch(typeof(TradeUtility))]
public static class Patch_TradeUtility
{
    /// <summary>
    ///     This patch allows Dsu's to Act as a trade beacon.
    ///     TODO! Options!
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(TradeUtility.AllLaunchableThingsForTrade))]
    public static void AllLaunchableThingsForTrade(Map map, ref IEnumerable<Thing> __result)
    {
        var yieldedThings = new HashSet<Thing>();
        yieldedThings.AddRange(__result);
        foreach (var dsu in map.GetAllPoweredDSU())
            yieldedThings.AddRange(dsu.GetStoredThings());

        __result = yieldedThings;
    }
}