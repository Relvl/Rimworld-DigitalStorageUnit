using System.Linq;
using DigitalStorageUnit.util;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

public class Building_AdvancedStorageUnitIOPort : Building_StorageUnitIOBase
{
    public override bool ShowLimitGizmo => false;

    public override StorageIOMode IOMode
    {
        get => StorageIOMode.Output;
        protected set { }
    }

    /// <summary>
    /// Disallow pawns to store items to this port
    /// </summary>
    public override bool ForbidPawnInput => true;

    public void MoveItem(Thing thing)
    {
        if (BoundStorageUnit is null) return;
        if (PowerTrader is null || !PowerTrader.PowerOn) return;
        thing.Position = WorkPosition;
    }

    public override void Tick()
    {
        if (BoundStorageUnit is null) return;
        foreach (var thing in Map.thingGrid.ThingsListAt(WorkPosition).ToList())
        {
            if (thing.def.category != ThingCategory.Item) continue;
            if (Map.reservationManager.AllReservedThings().Contains(thing)) continue;
            BoundStorageUnit.HandleNewItem(thing);
        }
    }
}