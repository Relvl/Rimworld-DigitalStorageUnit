using System.Collections.Generic;
using System.Linq;
using DigitalStorageUnit.map;
using DigitalStorageUnit.util;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

public class Building_AdvancedStorageUnitIOPort : Building_StorageUnitIOBase
{
    public override bool ShowLimitGizmo => false;

    private readonly List<Thing> _placementQueue = new();

    public void AddItemToQueue(Thing thing)
    {
        _placementQueue.Add(thing);
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

    public bool CanGetNewItem => GetstoredItem() == null && (PowerTrader?.PowerOn ?? false);

    public void updateQueue()
    {
        if (CanGetNewItem && _placementQueue.Count > 0)
        {
            var nextItemInQueue = _placementQueue[0];
            PlaceThingNow(nextItemInQueue);
            _placementQueue.RemoveAt(0);
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