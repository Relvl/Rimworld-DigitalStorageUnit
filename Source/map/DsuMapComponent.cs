using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.util;
using Verse;

namespace DigitalStorageUnit.map;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")] // All MapComponents are initiated when the colony map is created. The tick starts with the first world tick.
public class DsuMapComponent : MapComponent
{
    public HashSet<DigitalStorageUnitBuilding> DsuSet { get; } = new();

    public HashSet<Building_StorageUnitIOPort> IoPortSet { get; } = new();

    public HashSet<Building_AdvancedStorageUnitIOPort> AccessPointSet { get; } = new();

    public Dictionary<IntVec3, DigitalStorageUnitBuilding> DsuOccupiedPoints { get; } = new();

    public DsuMapComponent(Map map) : base(map)
    {
    }

    public void RegisterBuilding(Building building)
    {
        switch (building)
        {
            case DigitalStorageUnitBuilding dsu:
                DsuSet.Add(dsu);
                foreach (var point in dsu.OccupiedRect()) DsuOccupiedPoints[point] = dsu;
                break;
            case Building_AdvancedStorageUnitIOPort accessPoint:
                AccessPointSet.Add(accessPoint);
                break;
            case Building_StorageUnitIOPort ioport:
                IoPortSet.Add(ioport);
                break;
        }
    }

    public void DeregisterBuilding(Building building)
    {
        switch (building)
        {
            case DigitalStorageUnitBuilding dsu:
                DsuSet.Remove(dsu);
                foreach (var point in dsu.OccupiedRect()) DsuOccupiedPoints.Remove(point);
                break;
            case Building_AdvancedStorageUnitIOPort accessPoint:
                AccessPointSet.Remove(accessPoint);
                break;
            case Building_StorageUnitIOPort ioport:
                IoPortSet.Remove(ioport);
                break;
        }
    }

    public float GetTotalDistance(Pawn pawn, IntVec3 middlePos, IntVec3 destinationPos, bool isDsu = false)
    {
        // This is cheap, but just a direct distance without a walls etc
        // 1 Call ~ 0.2us
        if (DigitalStorageUnit.Config.CheapPathfinding)
        {
            return pawn.Position.DistanceTo(middlePos) + middlePos.DistanceTo(destinationPos) * (isDsu ? DigitalStorageUnit.Config.DsuPathingMultiplier : 1);
        }

        var secondPathCost = pawn.Map.pathFinder.FindPath(middlePos, destinationPos, pawn).TotalCost;
        if (DigitalStorageUnit.Config.HalfPathfinding) return secondPathCost;

        // TODO The issue with this is that it is extramly expencive.
        // TODO 1 Call ~ 0.4ms
        // TODO I Hope there is a better way to make this kind of a check
        // TODO maybe a manual calculation without the extra stepps included?
        return pawn.Map.pathFinder.FindPath(pawn.Position, middlePos, pawn).TotalCost + secondPathCost;
    }

    public DigitalStorageUnitBuilding GetDsuHoldingItem(Thing item) => DsuOccupiedPoints.TryGetValue(item.Position);

    public override void MapRemoved() => MapExtension.OnMapRemoved(map);
}