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
        UpdateQueue();
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        map.GetDsuComponent().RegisterBuilding(this);
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        Map.GetDsuComponent().DeregisterBuilding(this);
        base.DeSpawn(mode);
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

    public bool CanReceiveNewItem => GetstoredItem() == null && (PowerTrader?.PowerOn ?? false);

    public void UpdateQueue()
    {
        if (!CanReceiveNewItem || _placementQueue.Count <= 0) return;
        var nextItemInQueue = _placementQueue[0];
        PlaceThingNow(nextItemInQueue);
        _placementQueue.RemoveAt(0);
    }

    public void PlaceThingNow(Thing thing)
    {
        if (thing != null)
            thing.Position = Position;
    }

    public override void Tick()
    {
        UpdateQueue();

        if (!this.IsHashIntervalTick(10)) return;
        // If thing not reserved - draw item back to the DSU
        var thing = GetstoredItem();
        if (thing != null && !Map.reservationManager.AllReservedThings().Contains(thing))
            RefreshInput();
    }
}