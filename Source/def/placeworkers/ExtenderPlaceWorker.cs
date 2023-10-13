using System.Diagnostics.CodeAnalysis;
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
}