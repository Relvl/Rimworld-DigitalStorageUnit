using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DigitalStorageUnit.util;
using RimWorld;
using UnityEngine;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

/// <summary>
/// Same room
/// Line of sight
/// </summary>
[SuppressMessage("ReSharper", "UnusedType.Global")] // def-injected
public class ExtenderPlaceWorker : PlaceWorker
{
    public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
    {
        var room = loc.GetRoom(map);
        if (room is null || room.TouchesMapEdge) return "MustBePlacedIndoors".Translate();
        if (room.OpenRoofCount > 0) return "DSU.PlaceWorker.ProperRoomNeeded".Translate();
        var dsuInTheRoomCount = room.ContainedAndAdjacentThings.Count(t => t is DigitalStorageUnitBuilding);
        if (dsuInTheRoomCount <= 0) return "DSU.PlaceWorker.DsuNeeded".Translate();
        if (dsuInTheRoomCount > 1) return "DSU.PlaceWorker.OnlyOneDsuNeeded".Translate();
        return AcceptanceReport.WasAccepted;
    }

    public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
    {
        var room = center.GetRoom(Find.CurrentMap);
        if (room == null || room.UsesOutdoorTemperature) return;
        var dsuList = room.ContainedAndAdjacentThings.Where(t => t is DigitalStorageUnitBuilding).ToList();
        foreach (var dsu in dsuList)
        {
            GenDraw.DrawCircleOutline(dsu.TrueCenter(), TextureHolder.CircleRadius, SimpleColor.Yellow);
            GenDraw.DrawLineBetween(dsu.TrueCenter(), GenThing.TrueCenter(center, rot, def.size, def.Altitude), SimpleColor.Yellow, TextureHolder.LineWidth);
        }
    }
}