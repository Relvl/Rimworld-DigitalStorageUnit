using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DigitalStorageUnit.util;
using UnityEngine;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

[StaticConstructorOnStartup]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class Building_StorageUnitIOPort : Building_StorageUnitIOBase
{
    public override void RefreshInput()
    {
        if (!PowerTrader.PowerOn) return;
        var item = Position.GetFirstItem(Map);
        if (IOMode == StorageIOMode.Input && item != null && (BoundStorageUnit?.CanReciveThing(item) ?? false))
        {
            BoundStorageUnit.HandleNewItem(item);
        }
    }

    /// <summary>
    /// Modified version of Verse.Thing.TryAbsorbStack (based on 1.3.7964.22648) TODO! check new version
    /// Might Cause unexpected things as 
    /// DS Has a patch for Thing.TryAbsorbStack
    /// Thing.SplitOff has a CommonSense Transpiler
    /// </summary>
    /// <param name="baseThing"></param>
    /// <param name="toBeAbsorbed"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    private static bool AbsorbAmmount(ref Thing baseThing, ref Thing toBeAbsorbed, int count)
    {
        if (!baseThing.CanStackWith(toBeAbsorbed)) return false;
        if (baseThing.def.useHitPoints)
            baseThing.HitPoints = Mathf.CeilToInt((baseThing.HitPoints * baseThing.stackCount + toBeAbsorbed.HitPoints * count) / (float)(baseThing.stackCount + count));

        baseThing.stackCount += count;
        toBeAbsorbed.stackCount -= count;

        if (baseThing.Map != null) baseThing.DirtyMapMesh(baseThing.Map);

        StealAIDebugDrawer.Notify_ThingChanged(baseThing);

        if (baseThing.Spawned) baseThing.Map?.listerMergeables.Notify_ThingStackChanged(baseThing);

        if (toBeAbsorbed.stackCount <= 0)
        {
            toBeAbsorbed.Destroy();
            return true;
        }

        return false;
    }

    protected override void RefreshOutput()
    {
        if (!PowerTrader.PowerOn) return;

        var currentItem = Position.GetFirstItem(Map);
        var storageSlotAvailable = currentItem == null ||
                                   (settings.AllowedToAccept(currentItem) && OutputSettings.SatisfiesMax(currentItem.stackCount, currentItem.def.stackLimit));
        if (BoundStorageUnit is not { CanWork: true }) return;

        if (storageSlotAvailable)
        {
            var itemCandidates =
                new List<Thing>(from Thing t in BoundStorageUnit.StoredItems where settings.AllowedToAccept(t) select t); // ToList very important - evaluates enumerable
            //ItemsThatSatisfyMin somtimes spikes to 0.1 but it is mostly an none issue
            if (ItemsThatSatisfyMin(ref itemCandidates, currentItem))
            {
                foreach (var item in itemCandidates)
                {
                    if (currentItem != null)
                    {
                        if (currentItem.CanStackWith(item))
                        {
                            var count = Math.Min(item.stackCount, OutputSettings.CountNeededToReachMax(currentItem.stackCount, currentItem.def.stackLimit));
                            if (count > 0)
                            {
                                var thing = item;
                                //Merge Stacks - Gab count required to fulfill settings and merge them to the stuff on the IO Port
                                //For SplitOff "MakeThing" is expensive
                                //For TryAbsorbStack "Destroy" is expensive
                                AbsorbAmmount(ref currentItem, ref thing, count);
                                if (thing.stackCount <= 0) BoundStorageUnit.HandleMoveItem(thing);
                            }
                        }
                    }
                    else
                    {
                        var count = OutputSettings.CountNeededToReachMax(0, item.stackCount);
                        if (count > 0)
                        {
                            //Nothing on the IO Port - grab thing from storage and place it on the port
                            //For SplitOff "MakeThing" is expensive
                            var thingToRemove = item.SplitOff(count);
                            if (item.stackCount <= 0 || thingToRemove == item) BoundStorageUnit.HandleMoveItem(item);
                            currentItem = GenSpawn.Spawn(thingToRemove, Position, Map);
                        }
                    }

                    if (currentItem != null && !OutputSettings.SatisfiesMax(currentItem.stackCount, currentItem.def.stackLimit))
                    {
                        break;
                    }
                }
            }
        }

        //Transfre a item back if it is either too few or disallowed
        if (currentItem != null &&
            (!settings.AllowedToAccept(currentItem) || !OutputSettings.SatisfiesMin(currentItem.stackCount)) &&
            BoundStorageUnit.settings.AllowedToAccept(currentItem))
        {
            currentItem.SetForbidden(false, false);
            BoundStorageUnit.HandleNewItem(currentItem);
        }

        //Transfer the diffrence back if it is too much
        if (currentItem != null && !OutputSettings.SatisfiesMax(currentItem.stackCount, currentItem.def.stackLimit) && BoundStorageUnit.settings.AllowedToAccept(currentItem))
        {
            var splitCount = -OutputSettings.CountNeededToReachMax(currentItem.stackCount, currentItem.def.stackLimit);
            if (splitCount > 0)
            {
                var returnThing = currentItem.SplitOff(splitCount);
                returnThing.SetForbidden(false, false);
                BoundStorageUnit.HandleNewItem(returnThing);
            }
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var g in base.GetGizmos()) yield return g;

        yield return new Command_Action
        {
            defaultLabel = "DSU.IOMode".Translate() + ": " + (IOMode == StorageIOMode.Input ? "DSU.IOMode.Input".Translate() : "DSU.IOMode.Output".Translate()),
            action = () =>
            {
                Find.WindowStack.Add(
                    new FloatMenu(
                        new List<FloatMenuOption>
                        {
                            new("DSU.IOMode.Input".Translate(), () => SelectedPorts().ToList().ForEach(p => p.IOMode = StorageIOMode.Input)),
                            new("DSU.IOMode.Output".Translate(), () => SelectedPorts().ToList().ForEach(p => p.IOMode = StorageIOMode.Output))
                        }
                    )
                );
            },
            icon = TextureHolder.IoIcon
        };
        
    }

    private IEnumerable<Building_StorageUnitIOPort> SelectedPorts()
    {
        var selectedPorts = Find.Selector.SelectedObjects.OfType<Building_StorageUnitIOPort>().ToList();
        if (!selectedPorts.Contains(this)) selectedPorts.Add(this);
        return selectedPorts;
    }

    public override bool OutputItem(Thing thing)
    {
        if (BoundStorageUnit?.CanWork ?? false)
        {
            return GenPlace.TryPlaceThing(
                thing.SplitOff(thing.stackCount),
                Position,
                Map,
                ThingPlaceMode.Near,
                null,
                pos =>
                {
                    if (settings.AllowedToAccept(thing) && OutputSettings.SatisfiesMin(thing.stackCount))
                        if (pos == Position)
                            return true;
                    foreach (var t in Map.thingGrid.ThingsListAt(pos))
                    {
                        if (t is Building_StorageUnitIOPort) return false;
                    }

                    return true;
                }
            );
        }

        return false;
    }
}