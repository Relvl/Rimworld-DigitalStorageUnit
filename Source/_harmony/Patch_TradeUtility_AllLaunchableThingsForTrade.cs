using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DigitalStorageUnit._harmony;

/// <summary>
/// This patch allows Dsu's to Act as a trade beacon.
/// TODO! Options!
/// </summary>
[HarmonyPatch(typeof(TradeUtility), nameof(TradeUtility.AllLaunchableThingsForTrade))]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
internal static class Patch_TradeUtility_AllLaunchableThingsForTrade
{
    public static void Postfix(Map map, ref IEnumerable<Thing> __result)
    {
        var yieldedThings = new HashSet<Thing>();
        yieldedThings.AddRange(__result);
        foreach (var dsu in AllPowered(map))
        {
            yieldedThings.AddRange(dsu.StoredItems);
        }

        __result = yieldedThings;
    }

    public static IEnumerable<DigitalStorageUnitBuilding> AllPowered(Map map, bool any = false)
    {
        foreach (var building in map.listerBuildings.AllBuildingsColonistOfClass<DigitalStorageUnitBuilding>())
        {
            if (!building.Powered) continue;
            yield return building;
            if (any) break;
        }
    }
}