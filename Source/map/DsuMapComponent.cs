using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DigitalStorageUnit.extensions;
using Verse;

namespace DigitalStorageUnit.map;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")] // All MapComponents are initiated when the colony map is created. The tick starts with the first world tick.
public class DsuMapComponent(Map map) : MapComponent(map)
{
    public HashSet<DigitalStorageUnitBuilding> DsuSet { get; } = [];

    public HashSet<InputPortDsuBuilding> IoPortSet { get; } = [];

    public HashSet<AccessPointPortBuilding> AccessPointSet { get; } = [];

    public Dictionary<IntVec3, DigitalStorageUnitBuilding> DsuOccupiedPoints { get; } = new();

    public bool IsDSUOnPoint(IntVec3 point)
    {
        return DsuOccupiedPoints.ContainsKey(point);
    }

    public void RegisterBuilding(Building building)
    {
        switch (building)
        {
            case DigitalStorageUnitBuilding dsu:
                DsuSet.Add(dsu);
                foreach (var point in dsu.OccupiedRect()) DsuOccupiedPoints[point] = dsu;
                break;
            case AccessPointPortBuilding accessPoint:
                AccessPointSet.Add(accessPoint);
                break;
            case InputPortDsuBuilding ioport:
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
            case AccessPointPortBuilding accessPoint:
                AccessPointSet.Remove(accessPoint);
                break;
            case InputPortDsuBuilding ioport:
                IoPortSet.Remove(ioport);
                break;
        }
    }

    public float GetTotalDistance(Pawn pawn, IntVec3 middlePos, IntVec3 destinationPos, bool isDsu = false)
    {
        // This is cheap, but just a direct distance without a walls etc
        // 1 Call ~ 0.2us
        if (DigitalStorageUnit.Config.CheapPathfinding) 
            return pawn.Position.DistanceTo(middlePos) + middlePos.DistanceTo(destinationPos) * (isDsu ? DigitalStorageUnit.Config.DsuPathingMultiplier : 1);

        var secondPathCost = pawn.Map.pathFinder.FindPathNow(middlePos, destinationPos, pawn).TotalCost;
        if (DigitalStorageUnit.Config.HalfPathfinding) 
            return secondPathCost;

        // TODO The issue with this is that it is extremely expensive.
        // TODO 1 Call ~ 0.4ms
        // TODO I Hope there is a better way to make this kind of a check
        // TODO maybe a manual calculation without the extra steps included?
        return pawn.Map.pathFinder.FindPathNow(pawn.Position, middlePos, pawn).TotalCost + secondPathCost;
    }

    public DigitalStorageUnitBuilding GetDsuHoldingItem(Thing item)
    {
        return DsuOccupiedPoints.TryGetValue(item.Position);
    }

    public override void MapRemoved()
    {
        MapExtension.OnMapRemoved(map);
    }

    public IEnumerable<AccessPointPortBuilding> GetAccessPoints(DigitalStorageUnitBuilding dsu = null, bool powered = true)
    {
        return AccessPointSet.Where(ap =>
        {
            if (dsu != null && ap.BoundStorageUnit != dsu) return false;
            if (powered && !ap.Powered) return false;
            return true;
        });
    }
}