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

[StaticConstructorOnStartup]
public abstract class Building_StorageUnitIOBase : Building_Storage, IForbidPawnInputItem, ILwmDsLeaveMeAlonePlease
{
    private StorageIOMode _ioMode;
    private DigitalStorageUnitBuilding _linkedStorageParentBuilding;
    private StorageSettings _outputStoreSettings;
    private OutputSettings _outputSettings;

    protected virtual IntVec3 WorkPosition => Position; // todo all positions to work position

    public CompPowerTrader PowerTrader;

    public virtual bool ShowLimitGizmo => true;

    public override Graphic Graphic =>
        IOMode == StorageIOMode.Input
            ? base.Graphic.GetColoredVersion(base.Graphic.Shader, def.GetModExtension<ModExtensionPortColor>().inColor, Color.white)
            : base.Graphic.GetColoredVersion(base.Graphic.Shader, def.GetModExtension<ModExtensionPortColor>().outColor, Color.white);

    public virtual StorageIOMode IOMode
    {
        get => _ioMode;
        protected set
        {
            if (_ioMode == value) return;
            _ioMode = value;
            Notify_NeedRefresh();
        }
    }

    public DigitalStorageUnitBuilding BoundStorageUnit
    {
        get => _linkedStorageParentBuilding;
        set
        {
            // TODO! looks weird.
            _linkedStorageParentBuilding?.Ports.Remove(this);
            _linkedStorageParentBuilding = value;
            value?.Ports.Add(this);
            Notify_NeedRefresh();
        }
    }

    protected OutputSettings OutputSettings => _outputSettings ??= new OutputSettings("DSU.Min.Desc", "DSU.Max.Desc");

    //
    public virtual bool ForbidPawnInput
    {
        get
        {
            if (IOMode == StorageIOMode.Output && OutputSettings.UseMax)
            {
                // Only get currentItem if needed
                var currentItem = WorkPosition.GetFirstItem(Map);
                if (currentItem != null)
                {
                    return OutputSettings.CountNeededToReachMax(currentItem.stackCount, currentItem.def.stackLimit) <= 0;
                }
            }

            return false;
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref _ioMode, "mode");
        Scribe_References.Look(ref _linkedStorageParentBuilding, "boundStorageUnit");
        Scribe_Deep.Look(ref _outputStoreSettings, "outputStoreSettings", this);
        Scribe_Deep.Look(ref _outputSettings, "outputSettings", "DSU.Min.Desc", "DSU.Max.Desc");
    }

    public override string GetInspectString()
    {
        if (OutputSettings.UseMin && OutputSettings.UseMax)
            return base.GetInspectString() + "\n" + "DSU.Min.Current".Translate(OutputSettings.Min) + "\n" + "DSU.Max.Current".Translate(OutputSettings.Max);
        if (OutputSettings.UseMin && !OutputSettings.UseMax) return base.GetInspectString() + "\n" + "DSU.Min.Current".Translate(OutputSettings.Min);
        if (!OutputSettings.UseMin && OutputSettings.UseMax) return base.GetInspectString() + "\n" + "DSU.Max.Current".Translate(OutputSettings.Max);
        return base.GetInspectString();
    }

    public override void PostMake()
    {
        base.PostMake();
        PowerTrader = GetComp<CompPowerTrader>();
        _outputStoreSettings = new StorageSettings(this);
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        PowerTrader = GetComp<CompPowerTrader>();

        //TODO Issues occurs if the boundStorageUnit spawns after this... Needs a check form the other way
        if (BoundStorageUnit?.Map != map && (BoundStorageUnit?.Spawned ?? false))
        {
            BoundStorageUnit = null;
        }

        map.GetDsuComponent().RegisterBuilding(this);
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        BoundStorageUnit?.Ports.Remove(this);
        Map.GetDsuComponent().DeregisterBuilding(this);
        base.DeSpawn(mode);
    }

    protected override void ReceiveCompSignal(string signal)
    {
        base.ReceiveCompSignal(signal);
        if (signal == CompPowerTrader.PowerTurnedOnSignal) Notify_NeedRefresh();
    }

    public void Notify_NeedRefresh()
    {
        // RefreshStoreSettings
        if (_ioMode == StorageIOMode.Output)
        {
            settings = _outputStoreSettings;
            if (BoundStorageUnit != null && settings.Priority != BoundStorageUnit.settings.Priority)
            {
                //the setter of settings.Priority is expensive
                settings.Priority = BoundStorageUnit.settings.Priority;
            }
        }
        else if (BoundStorageUnit != null)
        {
            settings = BoundStorageUnit.settings;
        }
        else
        {
            settings = new StorageSettings(this);
        }

        switch (IOMode)
        {
            case StorageIOMode.Input:
                RefreshInput();
                break;
            case StorageIOMode.Output:
                RefreshOutput();
                break;
        }
    }

    public override void Notify_ReceivedThing(Thing newItem)
    {
        base.Notify_ReceivedThing(newItem);
        if (_ioMode == StorageIOMode.Input) RefreshInput();
    }

    public override void Notify_LostThing(Thing newItem)
    {
        base.Notify_LostThing(newItem);
        if (_ioMode == StorageIOMode.Output) RefreshOutput();
    }

    public override void Tick()
    {
        base.Tick();
        if (this.IsHashIntervalTick(10)) Notify_NeedRefresh(); // TODO! Oh my...
    }

    public virtual void RefreshInput()
    {
        if (PowerTrader.PowerOn)
        {
            var item = WorkPosition.GetFirstItem(Map);
            if (_ioMode == StorageIOMode.Input && item != null && (BoundStorageUnit?.CanReciveThing(item) ?? false))
            {
                BoundStorageUnit.HandleNewItem(item);
            }
        }
    }

    protected bool ItemsThatSatisfyMin(ref List<Thing> itemCandidates, Thing currentItem)
    {
        if (currentItem != null)
        {
            itemCandidates = itemCandidates.Where(currentItem.CanStackWith).ToList();
            var minReqierd = OutputSettings.UseMin ? _outputSettings.Min : 0;
            var count = currentItem.stackCount;
            var i = 0;
            while (i < itemCandidates.Count && count < minReqierd)
            {
                count += itemCandidates[i].stackCount;
                i++;
            }

            return OutputSettings.SatisfiesMin(count);
        }

        //I wonder if GroupBy is benifficial or not
        return itemCandidates.GroupBy(t => t.def).FirstOrDefault(g => OutputSettings.SatisfiesMin(g.Sum(t => t.stackCount)))?.Any() ?? false;
    }

    protected virtual void RefreshOutput() //
    {
        if (PowerTrader.PowerOn)
        {
            var currentItem = WorkPosition.GetFirstItem(Map);
            var storageSlotAvailable = currentItem == null ||
                                       (settings.AllowedToAccept(currentItem) && OutputSettings.SatisfiesMax(currentItem.stackCount, currentItem.def.stackLimit));
            if (BoundStorageUnit != null && BoundStorageUnit.CanReceiveIO)
            {
                if (storageSlotAvailable)
                {
                    var itemCandidates =
                        new List<Thing>(from Thing t in BoundStorageUnit.StoredItems where settings.AllowedToAccept(t) select t); // ToList very important - evaluates enumerable
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
                                        var thingToRemove = item.SplitOff(count);
                                        if (item.stackCount <= 0) BoundStorageUnit.HandleMoveItem(item);
                                        currentItem.TryAbsorbStack(thingToRemove, true);
                                    }
                                }
                            }
                            else
                            {
                                var count = OutputSettings.CountNeededToReachMax(0, item.stackCount);
                                if (count > 0)
                                {
                                    var thingToRemove = item.SplitOff(count);
                                    if (item.stackCount <= 0) BoundStorageUnit.HandleMoveItem(item);
                                    currentItem = GenSpawn.Spawn(thingToRemove, WorkPosition, Map);
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
                if (currentItem != null &&
                    !OutputSettings.SatisfiesMax(currentItem.stackCount, currentItem.def.stackLimit) &&
                    BoundStorageUnit.settings.AllowedToAccept(currentItem))
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
        }
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var g in base.GetGizmos()) yield return g;

        yield return new Command_Action
        {
            defaultLabel = "DSU.ItemSource".Translate() + ": " + (BoundStorageUnit?.LabelCap ?? "NoneBrackets".Translate()),
            action = () =>
            {
                var options = Map.listerBuildings.allBuildingsColonist //
                    .Where(b => b is DigitalStorageUnitBuilding)
                    .Select(b => new FloatMenuOption(b.LabelCap, () => SelectedPorts().ToList().ForEach(p => p.BoundStorageUnit = b as DigitalStorageUnitBuilding)))
                    .ToList();
                if (options.Count == 0)
                    options.Add(new FloatMenuOption("NoneBrackets".Translate(), null));
                Find.WindowStack.Add(new FloatMenu(options));
            },
            icon = TextureHolder.CargoPlatform
        };

        if (IOMode == StorageIOMode.Output && ShowLimitGizmo)
        {
            yield return new Command_Action
            {
                icon = TextureHolder.SetTargetFuelLevel,
                defaultLabel = "DSU.Output.Settings".Translate(),
                action = () => Find.WindowStack.Add(
                    new OutputMinMaxDialog(
                        OutputSettings,
                        () => SelectedPorts().Where(p => p.IOMode == StorageIOMode.Output).ToList().ForEach(p => OutputSettings.Copy(p.OutputSettings))
                    )
                )
            };
        }
    }

    private IEnumerable<Building_StorageUnitIOBase> SelectedPorts()
    {
        var selectedPorts = Find.Selector.SelectedObjects.OfType<Building_StorageUnitIOBase>().ToList();
        if (!selectedPorts.Contains(this)) selectedPorts.Add(this);
        return selectedPorts;
    }

    public virtual bool OutputItem(Thing thing)
    {
        if (BoundStorageUnit?.CanReceiveIO ?? false)
        {
            return GenPlace.TryPlaceThing(
                thing.SplitOff(thing.stackCount),
                WorkPosition,
                Map,
                ThingPlaceMode.Near,
                null,
                pos =>
                {
                    if (settings.AllowedToAccept(thing) && OutputSettings.SatisfiesMin(thing.stackCount))
                        if (pos == WorkPosition)
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

    public bool NoConnectionAlert => PowerTrader.PowerOn && BoundStorageUnit is null;
}