using System.Diagnostics.CodeAnalysis;
using System.Linq;
using UnityEngine;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class DsuHeaterPlaceWorker : PlaceWorker
{
    public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
    {
        if (!DigitalStorageUnit.Config.HeaterEnabled) return AcceptanceReport.WasAccepted;
        var room = loc.GetRoom(map);
        if (room is null || room.TouchesMapEdge) return "MustBePlacedIndoors".Translate();
        if (room.OpenRoofCount > 0) return "DSU.PlaceWorker.ProperRoomNeeded".Translate();
        return AcceptanceReport.WasAccepted;
    }

    public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
    {
        var room = center.GetRoom(Find.CurrentMap);
        if (room == null || room.UsesOutdoorTemperature) return;
        GenDraw.DrawFieldEdges(room.Cells.ToList(), GenTemperature.ColorRoomHot);
    }
}