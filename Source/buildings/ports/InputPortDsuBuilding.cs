using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DigitalStorageUnit.util;
using RimWorld;
using UnityEngine;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

[StaticConstructorOnStartup]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class InputPortDsuBuilding : ABasePortDsuBuilding
{
    public override StorageIOMode IOMode => StorageIOMode.Input;

    public override Graphic Graphic => base.Graphic.GetColoredVersion(base.Graphic.Shader, def.GetModExtension<ModExtensionPortColor>().inColor, Color.white);

    public override void Notify_ReceivedThing(Thing newItem)
    {
        base.Notify_ReceivedThing(newItem);
        SuckFirstItem();
    }

    public override void Tick()
    {
        if (!this.IsHashIntervalTick(10)) return;
        if (!Powered) return;
        if (BoundStorageUnit is null || !BoundStorageUnit.CanWork) return;
        settings = BoundStorageUnit.settings ?? new StorageSettings(this);
        SuckFirstItem();
    }

    private void SuckFirstItem()
    {
        foreach (var thing in Map.thingGrid.ThingsListAt(WorkPosition).ToList())
        {
            if (thing.def.category != ThingCategory.Item) continue;
            if (!BoundStorageUnit.CanReciveThing(thing)) continue;
            if (Map.reservationManager.AllReservedThings().Contains(thing)) continue;
            BoundStorageUnit.HandleNewItem(thing);
            FleckMaker.ThrowLightningGlow(WorkPosition.ToVector3(), Map, 0.8f);
            return;
        }
    }
}