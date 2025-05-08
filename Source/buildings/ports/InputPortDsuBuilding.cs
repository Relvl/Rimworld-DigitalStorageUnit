using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DigitalStorageUnit.compat;
using DigitalStorageUnit.util;
using RimWorld;
using UnityEngine;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

[StaticConstructorOnStartup]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class InputPortDsuBuilding : ABasePortDsuBuilding, IRemoveStorageInspectionTab
{
    private PortPositionComp _portPosition;
    private readonly Dictionary<IntVec3, int> _tickCooldown = new();

    public override StorageIOMode IOMode => StorageIOMode.Input;

    public override Graphic Graphic => base.Graphic.GetColoredVersion(base.Graphic.Shader, def.GetModExtension<ModExtensionPortColor>().inColor, Color.white);

    public override void Notify_ReceivedThing(Thing newItem)
    {
        base.Notify_ReceivedThing(newItem);
        SuckFirstItem(newItem.Position);
    }

    public override void PostMake()
    {
        base.PostMake();
        _portPosition ??= GetComp<PortPositionComp>();
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        _portPosition ??= GetComp<PortPositionComp>();
    }

    public override void Tick()
    {
        base.Tick();

        for (var i = _tickCooldown.Count - 1; i >= 0; i--)
        {
            var (key, value) = _tickCooldown.ElementAt(i);
            if (value == 1) _tickCooldown.Remove(key);
            else _tickCooldown[key] = value - 1;
        }

        if (!Powered) return;
        if (BoundStorageUnit is null) return;
        if (!BoundStorageUnit.CanWork) return;

        settings = BoundStorageUnit.settings ?? new StorageSettings(this);

        foreach (var position in _portPosition.GetAvailablePositions())
        {
            if (_tickCooldown.ContainsKey(position)) continue;
            SuckFirstItem(position);
        }
    }

    private void SuckFirstItem(IntVec3 position)
    {
        foreach (var thing in Map.thingGrid.ThingsListAt(position).ToList())
        {
            if (!thing.def.EverStorable(false)) continue;
            if (!BoundStorageUnit.CanReciveThing(thing)) continue;
            if (Map.reservationManager.AllReservedThings().Contains(thing)) continue;
            BoundStorageUnit.HandleNewItem(thing);
            FleckMaker.ThrowLightningGlow(position.ToVector3(), Map, 0.8f);
            return;
        }
    }
}