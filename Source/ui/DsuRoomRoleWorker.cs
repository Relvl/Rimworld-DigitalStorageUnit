using System.Diagnostics.CodeAnalysis;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

[SuppressMessage("ReSharper", "UnusedType.Global")] // def-injected
public class DsuRoomRoleWorker : RoomRoleWorker
{
    public override float GetScore(Room room)
    {
        if (!DigitalStorageUnit.Config.HeaterEnabled) return 0;

        var things = room.ContainedAndAdjacentThings;
        var dsuCount = things.Count(t => t is DigitalStorageUnitBuilding);

        return 10000 * dsuCount;
    }
}