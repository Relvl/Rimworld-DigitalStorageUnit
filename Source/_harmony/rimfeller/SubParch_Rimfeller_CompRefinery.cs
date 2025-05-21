using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.def;
using RimWorld;
using Verse;

namespace DigitalStorageUnit._harmony.rimfeller;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class SubParch_Rimfeller_CompRefinery
{
    // Rimefeller.CompRefinery.get_AdjacentHoppers
    public static bool Prefix(Thing ___parent, ref List<Building_Storage> __result)
    {
        var map = ___parent.Map;
        if (map == null) return true;
        foreach (var vec in ___parent.OccupiedRect().AdjacentCellsCardinal)
        {
            var thing = vec.GetFirstThing(map, DsuDefOf.DSU_InputBus_Building);
            if (thing is not InputPortDsuBuilding port || port.BoundStorageUnit is null || !port.BoundStorageUnit.CanWork) continue;
            __result = [port];
            return false;
        }

        return true;
    }
}