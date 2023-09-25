using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using DigitalStorageUnit.util;
using UnityEngine;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

[StaticConstructorOnStartup]
public class Building_StorageUnitIOPort : Building_StorageUnitIOBase
{
    public override Graphic Graphic =>
        IOMode == StorageIOMode.Input
            ? base.Graphic.GetColoredVersion(base.Graphic.Shader, def.GetModExtension<ModExtensionPortColor>().inColor, Color.white)
            : base.Graphic.GetColoredVersion(base.Graphic.Shader, def.GetModExtension<ModExtensionPortColor>().outColor, Color.white);

    public override StorageIOMode IOMode
    {
        get => ioMode;
        set
        {
            if (ioMode == value) return;
            ioMode = value;
            Notify_NeedRefresh();
        }
    }

    public override void RefreshInput()
    {
        if (!PowerTrader.PowerOn) return;
        var item = Position.GetFirstItem(Map);
        if (ioMode == StorageIOMode.Input && item != null && (boundStorageUnit?.CanReciveThing(item) ?? false))
        {
            boundStorageUnit.HandleNewItem(item);
        }
    }

    /// <summary>
    /// Modified version of Verse.Thing.TryAbsorbStack (based on 1.3.7964.22648)
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
        if (!baseThing.CanStackWith(toBeAbsorbed))
        {
            return false;
        }

        var num = count;

        if (baseThing.def.useHitPoints)
        {
            baseThing.HitPoints = Mathf.CeilToInt((baseThing.HitPoints * baseThing.stackCount + toBeAbsorbed.HitPoints * num) / (float)(baseThing.stackCount + num));
        }

        baseThing.stackCount += num;
        toBeAbsorbed.stackCount -= num;
        if (baseThing.Map != null)
        {
            baseThing.DirtyMapMesh(baseThing.Map);
        }

        StealAIDebugDrawer.Notify_ThingChanged(baseThing);
        if (baseThing.Spawned)
        {
            baseThing.Map.listerMergeables.Notify_ThingStackChanged(baseThing);
        }

        if (toBeAbsorbed.stackCount <= 0)
        {
            toBeAbsorbed.Destroy();
            return true;
        }

        return false;
    }

    protected override void RefreshOutput()
    {
        if (PowerTrader.PowerOn)
        {
            var currentItem = Position.GetFirstItem(Map);
            var storageSlotAvailable = currentItem == null ||
                                       (settings.AllowedToAccept(currentItem) && OutputSettings.SatisfiesMax(currentItem.stackCount, currentItem.def.stackLimit));
            if (boundStorageUnit != null && boundStorageUnit.CanReceiveIO)
            {
                if (storageSlotAvailable)
                {
                    var itemCandidates =
                        new List<Thing>(from Thing t in boundStorageUnit.StoredItems where settings.AllowedToAccept(t) select t); // ToList very important - evaluates enumerable
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
                                        var Mything = item;
                                        //Merge Stacks - Gab count required to fulfill settings and merge them to the stuff on the IO Port
                                        //For SplitOff "MakeThing" is expensive
                                        //For TryAbsorbStack "Destroy" is expensive
                                        AbsorbAmmount(ref currentItem, ref Mything, count);
                                        if (Mything.stackCount <= 0) boundStorageUnit.HandleMoveItem(Mything);
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
                                    var ThingToRemove = item.SplitOff(count);
                                    if (item.stackCount <= 0 || ThingToRemove == item) boundStorageUnit.HandleMoveItem(item);
                                    currentItem = GenSpawn.Spawn(ThingToRemove, Position, Map);
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
                    boundStorageUnit.settings.AllowedToAccept(currentItem))
                {
                    currentItem.SetForbidden(false, false);
                    boundStorageUnit.HandleNewItem(currentItem);
                }

                //Transfer the diffrence back if it is too much
                if (currentItem != null &&
                    (!OutputSettings.SatisfiesMax(currentItem.stackCount, currentItem.def.stackLimit) && boundStorageUnit.settings.AllowedToAccept(currentItem)))
                {
                    var splitCount = -OutputSettings.CountNeededToReachMax(currentItem.stackCount, currentItem.def.stackLimit);
                    if (splitCount > 0)
                    {
                        var returnThing = currentItem.SplitOff(splitCount);
                        returnThing.SetForbidden(false, false);
                        boundStorageUnit.HandleNewItem(returnThing);
                    }
                }
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
        var l = Find.Selector.SelectedObjects.Where(o => o is Building_StorageUnitIOPort).Select(o => (Building_StorageUnitIOPort)o).ToList();
        if (!l.Contains(this))
        {
            l.Add(this);
        }

        return l;
    }

    public override bool OutputItem(Thing thing)
    {
        if (boundStorageUnit?.CanReceiveIO ?? false)
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