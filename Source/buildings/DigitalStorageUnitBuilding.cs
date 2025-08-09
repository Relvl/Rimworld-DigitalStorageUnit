using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using DigitalStorageUnit.compat;
using DigitalStorageUnit.extensions;
using DigitalStorageUnit.ui;
using DigitalStorageUnit.util;
using RimWorld;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

[StaticConstructorOnStartup]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")] // def-reflected
public class DigitalStorageUnitBuilding : Building_Storage, IForbidPawnInputItem, IHoldMultipleThings.IHoldMultipleThings, ILwmDsLeaveMeAlonePlease, IRenameable
{
    public readonly HashSet<DataExtenderBuilding> Extenders = [];
    private CompPowerTrader _compPowerTrader;
    private DsuHeaterComp _heaterComp;
    private bool _pawnAccess = true;
    private int _storedItemsCount;

    public HashSet<ABasePortDsuBuilding> Ports = [];
    public string UniqueName;

    // Todo! Technology
    private float Efficiency => DigitalStorageUnit.Config.HeaterEnabled ? 1.4f : 1.0f;

    public int GetSlotLimit => (int)(( DigitalStorageUnit.Config.BaseDsuStackSize + Extenders.Count(e => e.Powered) * DigitalStorageUnit.Config.ExtenderStackIncrease) * Efficiency);
    public bool Powered => _compPowerTrader?.PowerOn ?? false;
    public bool CanWork => Powered && Spawned && _heaterComp.IsRoomHermetic();

    public override string LabelNoCount => UniqueName ?? base.LabelNoCount;
    public override string LabelCap => UniqueName ?? base.LabelCap;

    public bool ForbidPawnInput => !_pawnAccess || GetSlotLimit <= _storedItemsCount;

    public override void Notify_ReceivedThing(Thing newItem)
    {
        base.Notify_ReceivedThing(newItem);
        RearrangeItems();
        UpdatePowerConsumption();
    }

    public override void Notify_LostThing(Thing newItem)
    {
        base.Notify_LostThing(newItem);
        RearrangeItems();
        UpdatePowerConsumption();
    }

    /// <summary>
    ///     Pick Up And Haul compatibility.
    ///     <returns>true if can store, capacity is how many can store (more than one stack possible)</returns>
    /// </summary>
    public bool CapacityAt(Thing thing, IntVec3 cell, Map map, out int capacity)
    {
        capacity = 0;
        if (thing is null || map is null || map != Map || !CanWork) return false;
        thing = thing.GetInnerIfMinified();
        if (!Accepts(thing)) return false;
        foreach (var storedItem in GetStoredThings())
        {
            if (storedItem.def != thing.def || (thing.def.MadeFromStuff && storedItem.Stuff != thing.Stuff)) continue;
            capacity += thing.def.stackLimit - storedItem.stackCount;
        }

        capacity += (GetSlotLimit - _storedItemsCount) * thing.def.stackLimit;
        return capacity > 0;
    }

    public bool StackableAt(Thing thing, IntVec3 cell, Map map)
    {
        return CapacityAt(thing, cell, map, out _);
    }

    public string RenamableLabel
    {
        get => UniqueName ?? LabelCapNoCount;
        set => UniqueName = value;
    }

    public string BaseLabel => LabelCapNoCount;
    public string InspectLabel => LabelCap;

    public bool CanReceiveThing(Thing item)
    {
        return CanWork && GetSlotLimit > _storedItemsCount && Accepts(item);
    }

    public List<Thing> GetStoredThings() => Map.thingGrid.ThingsListAtFast(Position).Where(t => t.def.EverStorable(false) && t != this).ToList();

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref Ports, "ports", LookMode.Reference);
        Scribe_Values.Look(ref UniqueName, "uniqueName");
        Scribe_Values.Look(ref _pawnAccess, "pawnAccess", true);
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);

        _compPowerTrader ??= GetComp<CompPowerTrader>();
        _heaterComp ??= GetComp<DsuHeaterComp>();

        map.GetDsuComponent().RegisterBuilding(this);

        // Detaching ports in another maps
        foreach (var port in Ports)
        {
            if (!(port?.Spawned ?? false)) continue;
            if (port.Map != map) port.BoundStorageUnit = null;
        }

        RearrangeItems();
    }

    public override void PostMapInit()
    {
        base.PostMapInit();
        _compPowerTrader ??= GetComp<CompPowerTrader>();
        _heaterComp ??= GetComp<DsuHeaterComp>();
        RearrangeItems();
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        // todo! another types? destroying by damage?
        foreach (var thing in GetStoredThings())
            if (mode == DestroyMode.Deconstruct)
            {
                thing.Destroy();
            }
            else
            {
                Log.Warning($"DSU despawn mode: {mode}");

                // Unbulk the items
                thing.DeSpawn();
                GenPlace.TryPlaceThing(thing, Position, Map, ThingPlaceMode.Near);
            }

        Map.GetDsuComponent().DeregisterBuilding(this);
        base.DeSpawn(mode);
    }

    private void RearrangeItems()
    {
        foreach (var cell in AllSlotCells())
        {
            if (cell == Position) continue;
            foreach (var thing in Map.thingGrid.ThingsListAtFast(cell).ToList())
            {
                if (!thing.def.EverStorable(false)) continue;
                if (thing == this) continue;

                if (thing.Position != Position)
                    HandleNewItem(thing);
            }
        }

        foreach (var tabItems in GetInspectTabs().OfType<ITab_Items>())
            tabItems.RecalculateList();
    }

    public override void Tick()
    {
        base.Tick();
        _storedItemsCount = GetStoredThings().Count;
    }

    public void HandleNewItem(Thing item, bool tryAbsorb = true)
    {
        if (item.Destroyed) return;
        if (tryAbsorb)
            foreach (var storedThing in GetStoredThings())
            {
                if (storedThing == item) continue;
                storedThing.TryAbsorbStack(item, true);
                if (item.Destroyed) return;
            }

        if (!CanReceiveThing(item)) return;
        item.Position = Position;
        if (!item.Spawned) item.SpawnSetup(Map, false);

        if (item.def.drawerType is DrawerType.MapMeshAndRealTime or DrawerType.RealtimeOnly)
            Map.dynamicDrawManager.DeRegisterDrawable(item);
    }

    private void UpdatePowerConsumption()
    {
        _compPowerTrader.powerOutputInt = -1 * _storedItemsCount * DigitalStorageUnit.Config.EnergyPerStack * (DigitalStorageUnit.Config.HeaterEnabled ? 0.9f : 1f);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var g in base.GetGizmos()) yield return g;

        yield return new Command_Action
        {
            icon = TextureHolder.Rename,
            action = () => Find.WindowStack.Add(new RenameDsuDialog(this)),
            hotKey = KeyBindingDefOf.Misc1,
            defaultLabel = "DSU.Rename".Translate(),
            defaultDesc = "DSU.Rename.Desc".Translate()
        };

        yield return new Command_Toggle
        {
            isActive = () => _pawnAccess,
            toggleAction = () => _pawnAccess = !_pawnAccess,
            icon = TextureHolder.StoragePawnAccessSwitchIcon,
            defaultLabel = "DSU.PawnAccess".Translate(),
            defaultDesc = "DSU.PawnAccess.Desc".Translate()
        };
    }

    public override string GetInspectString()
    {
        var original = base.GetInspectString();
        var stringBuilder = new StringBuilder();
        if (!string.IsNullOrEmpty(original)) stringBuilder.AppendLine(original);
        stringBuilder.Append("DSU.TotalStackNum".Translate(_storedItemsCount));
        return stringBuilder.ToString();
    }

    /// <summary>
    ///     Render name and stack sount
    /// </summary>
    public override void DrawGUIOverlay()
    {
        base.DrawGUIOverlay();
        if (Current.CameraDriver.CurrentZoom > CameraZoomRange.Close) return;
        GenMapUI.DrawThingLabel(this, LabelCap + "\n\r" + "DSU.StacksCount.Detailed".Translate(_storedItemsCount, GetSlotLimit));
    }
}