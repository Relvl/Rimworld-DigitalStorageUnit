using System;
using System.Collections.Generic;
using System.Linq;
using DigitalStorageUnit.ui;
using DigitalStorageUnit.util;
using RimWorld;
using UnityEngine;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

/// <summary>
/// Provides items to the map.
/// Respects storage settings and limits.
/// Suck excess and unrecpected items back to the DSU (so it is Input Bus also?)
/// 
/// Todo! Allow to output above, to any direction, to all directions
/// Todo! Mode texture above
/// Todo! Enable forbid output
/// Todo! Alert about non-zoned output if not forbidden
/// Todo! Patch "unforbid all" if the item was "spawned" by the port? 
/// Todo! Enable whole zone output - maaaaaybe after research?
/// </summary>
public class OutputPortDsuBuilding : ABasePortDsuBuilding
{
    private OutputSettings _outputSettings;
    private PortPositionComp _portPosition;
    private readonly Dictionary<IntVec3, int> _tickCooldown = new();

    public override StorageIOMode IOMode => StorageIOMode.Output;

    private OutputSettings OutputSettings => _outputSettings ??= new OutputSettings();

    /// <summary>
    /// Pawns can't place there
    /// </summary>
    public override bool ForbidPawnInput => true;

    /// <summary>
    /// Todo! Mode texture above
    /// </summary>
    public override Graphic Graphic => base.Graphic.GetColoredVersion(base.Graphic.Shader, def.GetModExtension<ModExtensionPortColor>().outColor, Color.white);

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

    private void OnOutputSettingsAction() => Find.WindowStack.Add(new OutputMinMaxDialog(OutputSettings, OnOutputSettingsClosed));

    // Todo! Clear this
    private void OnOutputSettingsClosed() => SelectedPorts().ToList().ForEach(p => OutputSettings.Copy(p.OutputSettings));

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

        // Todo! Sync storage area at positions

        foreach (var position in _portPosition.GetAvailablePositions())
        {
            if (_tickCooldown.ContainsKey(position)) continue;
            var thingsAtPosition = position.GetThingList(Map).Where(t => t.def.EverStorable(false)).ToList();

            if (thingsAtPosition.Count == 0)
            {
                RefillEmptySlot(position);
                continue;
            }

            for (var idx = thingsAtPosition.Count - 1; idx >= 0; idx--)
            {
                var thing = thingsAtPosition[idx];
                if (!thing.def.EverStorable(false)) continue;
                if (Map.reservationManager.AllReservedThings().Contains(thing)) continue;
                if (SuckDisalowed(thing, position)) continue;
                if (SuckMoreThanFirst(thing, idx, position)) continue;
                if (SuckMoreThanMax(thing, position)) continue;
                if (SuckLessThanMin(thing, position)) continue;
            }
        }
    }

    private void RefillEmptySlot(IntVec3 position)
    {
        if (!position.Walkable(Map)) return;
        if (!position.InBounds(Map)) return;
        foreach (var thing in Map.thingGrid.ThingsListAt(position).ToList())
        {
            if (thing is DigitalStorageUnitBuilding) return;
            if (thing is InputPortDsuBuilding) return;
            // Todo! Another unsupported building?
        }

        foreach (var thing in BoundStorageUnit.GetStoredThings().Where(t => settings.AllowedToAccept(t)).ToList())
        {
            var count = thing.stackCount;
            if (OutputSettings.UseMax) count = Math.Min(count, OutputSettings.Max);
            if (OutputSettings.UseMin && count < OutputSettings.Min) continue;

            var splitedThing = ThingMaker.MakeThing(thing.def, thing.Stuff);
            splitedThing.stackCount = count;
            thing.stackCount -= count;
            if (thing.def.useHitPoints) splitedThing.HitPoints = thing.HitPoints;

            GenSpawn.Spawn(splitedThing, position, Map, Rot4.East);
            FleckMaker.ThrowLightningGlow(position.ToVector3(), Map, 0.8f);

            if (thing.stackCount <= 0)
            {
                if (thing.holdingOwner != null) thing.holdingOwner.Remove(thing);
                thing.DirtyMapMesh(thing.Map);
                Map.listerMergeables.Notify_ThingStackChanged(thing);
                thing.Destroy();
            }

            return;
        }
    }

    private bool SuckDisalowed(Thing thing, IntVec3 position)
    {
        if (settings.AllowedToAccept(thing)) return false;
        _tickCooldown[position] = 20;
        if (!BoundStorageUnit.CanReciveThing(thing))
        {
            _tickCooldown[position] = 20;
            return true;
        }

        BoundStorageUnit.HandleNewItem(thing);
        return true;
    }

    private bool SuckMoreThanFirst(Thing thing, int idx, IntVec3 position)
    {
        if (idx == 0) return false;
        if (!BoundStorageUnit.CanReciveThing(thing))
        {
            _tickCooldown[position] = 20;
            return true;
        }

        BoundStorageUnit.HandleNewItem(thing);
        return true;
    }

    private bool SuckMoreThanMax(Thing thing, IntVec3 position)
    {
        if (!OutputSettings.UseMax) return false;
        var max = Math.Min(OutputSettings.Max, thing.def.stackLimit);
        if (thing.stackCount <= max) return false;
        if (!BoundStorageUnit.CanReciveThing(thing))
        {
            _tickCooldown[position] = 20;
            return true;
        }

        var splitOffCount = thing.stackCount - max; // to split out

        // Merging without absorb // Todo! so dirty...
        var splitedThing = ThingMaker.MakeThing(thing.def, thing.Stuff);
        splitedThing.stackCount = splitOffCount;
        splitedThing.Position = thing.Position;
        thing.stackCount -= splitOffCount;
        thing.DirtyMapMesh(thing.Map);
        if (thing.def.useHitPoints) splitedThing.HitPoints = thing.HitPoints;
        splitedThing.SpawnSetup(thing.Map, false);
        BoundStorageUnit.HandleNewItem(splitedThing);
        return true;
    }

    private bool SuckLessThanMin(Thing thing, IntVec3 position)
    {
        if (!OutputSettings.UseMin) return false;
        var min = Math.Min(OutputSettings.Min, thing.def.stackLimit);
        if (thing.stackCount >= min) return false;
        if (!BoundStorageUnit.CanReciveThing(thing))
        {
            _tickCooldown[position] = 20;
            return true;
        }

        BoundStorageUnit.HandleNewItem(thing);
        return true;
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var g in base.GetGizmos()) yield return g;

        yield return new Command_Action { icon = TextureHolder.SetTargetFuelLevel, defaultLabel = "DSU.Output.Settings".Translate(), action = OnOutputSettingsAction };
    }

    public override string GetInspectString()
    {
        var result = base.GetInspectString();
        if (OutputSettings.UseMin || OutputSettings.UseMax)
        {
            var min = OutputSettings.UseMin ? OutputSettings.Min.ToString("0") : "1";
            var max = OutputSettings.UseMax ? OutputSettings.Max.ToString("0") : "∞";
            result += "\n";
            result += "DSU.Limit.GetInspectString".Translate(min, max);
        }

        return result;
    }

    private IEnumerable<OutputPortDsuBuilding> SelectedPorts()
    {
        var selectedPorts = Find.Selector.SelectedObjects.OfType<OutputPortDsuBuilding>().ToList();
        if (!selectedPorts.Contains(this)) selectedPorts.Add(this);
        return selectedPorts;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref _outputSettings, "outputSettings");
    }
}