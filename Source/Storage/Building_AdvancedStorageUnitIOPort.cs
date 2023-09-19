using System.Collections.Generic;
using System.Linq;
using DigitalStorageUnit.Common;
using Verse;

namespace DigitalStorageUnit.Storage;

public class Building_AdvancedStorageUnitIOPort : Building_StorageUnitIOBase
{
    public override bool ShowLimitGizmo => false;

    private List<Thing> placementQueue = new();

    public void AddItemToQueue(Thing thing)
    {
        placementQueue.Add(thing);
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        Map.GetDsuComponent().AdvancedPortLocations.Remove(Position);
        base.DeSpawn(mode);
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        map.GetDsuComponent().AdvancedPortLocations.TryAdd(Position, this);
    }

    public override StorageIOMode IOMode
    {
        get => StorageIOMode.Output;
        set { }
    }

    public override bool ForbidPawnInput => true;

    private Thing GetstoredItem()
    {
        var map = Map;
        if (map is null)
        {
            Log.Error($"PRF GetstoredItem @{Position} map is null");
            return null;
        }

        return WorkPosition.GetFirstItem(Map);
    }

    public bool CanGetNewItem => GetstoredItem() == null && (powerComp?.PowerOn ?? false);

    public override bool IsAdvancedPort => true;

    public void updateQueue()
    {
        if (CanGetNewItem && placementQueue.Count > 0)
        {
            var nextItemInQueue = placementQueue[0];
            PlaceThingNow(nextItemInQueue);
            placementQueue.RemoveAt(0);
        }
    }

    public void PlaceThingNow(Thing thing)
    {
        if (thing != null)
        {
            thing.Position = Position;
        }
    }

    public override void Tick()
    {
        updateQueue();

        if (this.IsHashIntervalTick(10))
        {
            var thing = GetstoredItem();
            if (thing != null && !Map.reservationManager.AllReservedThings().Contains(thing))
            {
                RefreshInput();
            }
        }
    }
}