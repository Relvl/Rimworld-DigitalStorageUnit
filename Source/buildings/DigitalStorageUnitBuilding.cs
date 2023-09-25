using RimWorld;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using DigitalStorageUnit.map;
using DigitalStorageUnit.ui;
using DigitalStorageUnit.util;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

[StaticConstructorOnStartup]
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")] // def-reflected
public class DigitalStorageUnitBuilding : Building_Storage, IForbidPawnInputItem, IHoldMultipleThings.IHoldMultipleThings
{
    private CompPowerTrader _compPowerTrader;
    private bool _pawnAccess = true;

    public List<Building_StorageUnitIOBase> Ports = new();
    public string UniqueName;

    public bool ForbidPawnInput => !_pawnAccess || !CanStoreMoreItems;

    public ModExtensionDsu Mod;

    private bool CanStoreMoreItems => (Powered) && Spawned && (Mod == null || StoredItems.Count < MaxNumberItemsInternal);

    private int MaxNumberItemsInternal => (Mod?.limit ?? int.MaxValue) - def.Size.Area + 1;

    public List<Thing> StoredItems { get; } = new();
    public bool Powered => _compPowerTrader?.PowerOn ?? false;
    public bool CanReceiveIO => Powered && Spawned;

    public override string LabelNoCount => UniqueName ?? base.LabelNoCount;
    public override string LabelCap => UniqueName ?? base.LabelCap;

    public override void Notify_ReceivedThing(Thing newItem)
    {
        base.Notify_ReceivedThing(newItem);
        if (newItem.Position != Position) HandleNewItem(newItem);
        RefreshStorage();
        UpdatePowerConsumption();
    }

    public override void Notify_LostThing(Thing newItem)
    {
        base.Notify_LostThing(newItem);
        StoredItems.Remove(newItem);
        RefreshStorage();
        UpdatePowerConsumption();
    }

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
        Mod ??= def.GetModExtension<ModExtensionDsu>();

        foreach (var cell in this.OccupiedRect().Cells)
        {
            var component = map.GetDsuComponent();
            component.HideItems.Add(cell);
            component.HideRightMenus.Add(cell);
        }

        RefreshStorage();

        // Detaching ports in another maps
        foreach (var port in Ports)
        {
            if (!(port?.Spawned ?? false)) continue;
            if (port.Map != map) port.BoundStorageUnit = null;
        }
    }

    public override void PostMapInit()
    {
        base.PostMapInit();
        _compPowerTrader ??= GetComp<CompPowerTrader>();
        RefreshStorage();
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        // todo! another types? destroying by damage?
        if (mode == DestroyMode.Deconstruct)
            if (def.GetModExtension<ModExtensionDsu>()?.destroyContainsItems ?? false)
                StoredItems.Where(t => !t.Destroyed).ToList().ForEach(x => x.Destroy());

        // Unbulk the items
        foreach (var thing in Position.GetThingList(Map).Where(thing => thing.def.category == ThingCategory.Item))
        {
            thing.DeSpawn();
            GenPlace.TryPlaceThing(thing, Position, Map, ThingPlaceMode.Near);
        }

        foreach (var cell in this.OccupiedRect().Cells)
        {
            var component = Map.GetDsuComponent();
            component.HideItems.Remove(cell);
            component.HideRightMenus.Remove(cell);
        }

        base.DeSpawn(mode);
    }

    //TODO! Why do we need to clear StoredItems here (and anywhere else)?
    private void RefreshStorage()
    {
        StoredItems.Clear();

        if (!Spawned) return;

        foreach (var cell in AllSlotCells())
        {
            foreach (var item in cell.GetThingList(Map))
            {
                if (item.def.category != ThingCategory.Item) continue;

                // TODO! Weird move... Why not just call HandleNewItem?
                if (cell != Position)
                    HandleNewItem(item);
                else
                {
                    if (StoredItems.Contains(item)) continue;
                    StoredItems.Add(item);
                    Map.dynamicDrawManager.DeRegisterDrawable(item);
                }
            }
        }
    }

    /// <summary>
    /// Pick Up And Haul compatibility.
    /// <returns>true if can store, capacity is how many can store (more than one stack possible)</returns>
    /// </summary>
    public bool CapacityAt(Thing thing, IntVec3 cell, Map map, out int capacity)
    {
        capacity = 0;

        if (thing == null || map == null || map != Map || !Spawned) return false;

        thing = thing.GetInnerIfMinified();

        //Check if thing can be stored based upon the storgae settings
        if (!Accepts(thing))
        {
            return false;
        }

        //TODO Check if we want to forbid access if power is off
        //if (!GetComp<CompPowerTrader>().PowerOn) return false;

        //Get List of items stored in the DSU
        var storedItems = Position.GetThingList(Map).Where(t => t.def.category == ThingCategory.Item);

        //Find the Stack size for the thing
        var maxstacksize = thing.def.stackLimit;
        //Get capacity of partial Stacks
        //  So 45 Steel and 75 Steel and 11 Steel give 30+64 more capacity for steel
        foreach (var partialStack in storedItems.Where(t => t.def == thing.def && t.stackCount < maxstacksize))
        {
            capacity += maxstacksize - partialStack.stackCount;
        }

        //capacity of empy slots
        capacity += (MaxNumberItemsInternal - storedItems.Count()) * maxstacksize;

        //Access point:
        if (cell != Position)
        {
            var maybeThing = Map.thingGrid.ThingAt(cell, ThingCategory.Item);
            if (maybeThing != null)
            {
                if (maybeThing.def == thing.def) capacity += (thing.def.stackLimit - maybeThing.stackCount);
            }
            else
            {
                capacity += thing.def.stackLimit;
            }
        }

        return capacity > 0;
    }

    public bool StackableAt(Thing thing, IntVec3 cell, Map map) => CapacityAt(thing, cell, map, out _);

    public void HandleNewItem(Thing item)
    {
        if (item.Destroyed) return;
        foreach (var storedThing in Position.GetThingList(Map))
        {
            if (storedThing == item) continue;
            storedThing.TryAbsorbStack(item, true);
            if (item.Destroyed) return;
        }

        if (!StoredItems.Contains(item))
        {
            StoredItems.Add(item);
        }

        // TODO! Why not in block above?
        if (CanStoreMoreItems) item.Position = Position;
        // TODO! WTF? How do we add despawned item?
        if (!item.Spawned) item.SpawnSetup(Map, false);

        Map.dynamicDrawManager.DeRegisterDrawable(item);
    }

    // TODO! WTF they means?..
    public void HandleMoveItem(Thing item)
    {
        //throw new System.NotImplementedException();
    }

    public bool CanReciveThing(Thing item) => settings.AllowedToAccept(item) && CanReceiveIO && CanStoreMoreItems;

    public bool HoldsPos(IntVec3 pos) => AllSlotCells()?.Contains(pos) ?? false;

    private void UpdatePowerConsumption() => _compPowerTrader.powerOutputInt = -1 * StoredItems.Count * /* todo! config! */ 10f;

    // TODO! I'm sure we can remove this refresh function...
    protected override void ReceiveCompSignal(string signal)
    {
        base.ReceiveCompSignal(signal);
        switch (signal)
        {
            case "PowerTurnedOn":
                RefreshStorage();
                break;
        }
    }

    // TODO! Just because? Power update is unstable without ticks?
    public override void Tick()
    {
        base.Tick();
        if (this.IsHashIntervalTick(60)) UpdatePowerConsumption();
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var g in base.GetGizmos())
            yield return g;

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

        if (Prefs.DevMode)
        {
            yield return new Command_Action
            {
                icon = TexUI.RotRightTex, action = RefreshStorage, defaultLabel = "DSU.Reorganize".Translate(), defaultDesc = "DSU.Reorganize.Desc".Translate()
            };
        }
    }

    public override string GetInspectString()
    {
        var original = base.GetInspectString();
        var stringBuilder = new StringBuilder();
        if (!string.IsNullOrEmpty(original)) stringBuilder.AppendLine(original);
        stringBuilder.Append("DSU.TotalStackNum".Translate(StoredItems.Count));
        return stringBuilder.ToString();
    }

    /// <summary>
    /// Render name and stack sount
    /// </summary>
    public override void DrawGUIOverlay()
    {
        base.DrawGUIOverlay();
        if (Current.CameraDriver.CurrentZoom > CameraZoomRange.Close) return;
        if (Mod is not null)
            GenMapUI.DrawThingLabel(this, LabelCap + "\n\r" + "DSU.StacksCount.Detailed".Translate(StoredItems.Count, def.GetModExtension<ModExtensionDsu>().limit));
        else
            GenMapUI.DrawThingLabel(this, LabelCap + "\n\r" + "DSU.StacksCount".Translate(StoredItems.Count));
    }
}