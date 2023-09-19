﻿using HarmonyLib;
using RimWorld;
using Verse;

namespace DigitalStorageUnit.Common.HarmonyPatches;

[HarmonyPatch(typeof(TradeDeal), "InSellablePosition")]
class Patch_TradeDeal_InSellablePosition
{
    public static bool Prefix(Thing t, out string reason, ref bool __result)
    {
        if (!t.Spawned && t.MapHeld != null)
        {
            foreach (var (pos, building) in t.MapHeld.GetDsuComponent().ColdStorageLocations)
            {
                if (building.StoredItems.Contains(t))
                {
                    reason = null;
                    __result = true;
                    return false;
                }
            }
        }
        else if (t.MapHeld is null)
        {
            Log.Warning($"Report to DigitalStorageUnit with HugsLog(CTRL & F12) - TradeDeal InSellablePosition {t}.MapHeld is Null");
        }

        reason = null;
        return true;
    }
}