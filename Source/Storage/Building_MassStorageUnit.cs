using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DigitalStorageUnit.Common;
using DigitalStorageUnit.Common.HarmonyPatches;
using DigitalStorageUnit.Storage.Editables;
using DigitalStorageUnit.Storage.UI;
using UnityEngine;
using Verse;

namespace DigitalStorageUnit.Storage;

[StaticConstructorOnStartup]
public abstract class Building_MassStorageUnit : Building_Storage, IForbidPawnInputItem, IRenameBuilding, ILinkableStorageParent, ILimitWatcher, IHoldMultipleThings.IHoldMultipleThings
{
    private static readonly Texture2D RenameTex = ContentFinder<Texture2D>.Get("UI/Buttons/Rename");

    private readonly List<Thing> _items = new();
    private List<Building_StorageUnitIOBase> _ports = new();
    private string _uniqueName;

    public string UniqueName
    {
        get => _uniqueName;
        set => _uniqueName = value;
    }

    public Building Building => this;
    public IntVec3 GetPosition => Position;
    public StorageSettings GetSettings => settings;

    public bool CanUseIOPort => def.GetModExtension<DefModExtension_CanUseStorageIOPorts>() != null;

    public LocalTargetInfo GetTargetInfo => this;

    //Initialized at spawn
    public DefModExtension_Crate ModExtensionCrate;

    public abstract bool CanStoreMoreItems { get; }

    // The maximum number of item stacks at this.Position:
    //  One item on each cell and the rest multi-stacked on Position?
    public int MaxNumberItemsInternal => (ModExtensionCrate?.limit ?? int.MaxValue) - def.Size.Area + 1;

    public List<Thing> StoredItems => _items;
    public int StoredItemsCount => _items.Count;
    public override string LabelNoCount => UniqueName ?? base.LabelNoCount;
    public override string LabelCap => UniqueName ?? base.LabelCap;
    public virtual bool CanReceiveIO => true;
    public virtual bool Powered => true;

    public virtual bool ForbidPawnInput => false;

    private StorageOutputUtil _outputUtil;

    public override void Notify_ReceivedThing(Thing newItem)
    {
        base.Notify_ReceivedThing(newItem);
        if (newItem.Position != Position) HandleNewItem(newItem);
        RefreshStorage();
    }

    public override void Notify_LostThing(Thing newItem)
    {
        base.Notify_LostThing(newItem);
        _items.Remove(newItem);
        ItemCountsRemoved(newItem.def, newItem.stackCount);
        RefreshStorage();
    }

    public bool AdvancedIOAllowed => true;

    public void DeregisterPort(Building_StorageUnitIOBase port) => _ports.Remove(port);
    public void RegisterPort(Building_StorageUnitIOBase port) => _ports.Add(port);

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var g in base.GetGizmos())
            yield return g;
        yield return new Command_Action
        {
            icon = RenameTex,
            action = () => Find.WindowStack.Add(new Dialog_RenameMassStorageUnit(this)),
            hotKey = KeyBindingDefOf.Misc1,
            defaultLabel = "PRFRenameMassStorageUnitLabel".Translate(),
            defaultDesc = "PRFRenameMassStorageUnitDesc".Translate()
        };
        yield return new Command_Action
        {
            icon = TexUI.RotRightTex,
            action = () =>
            {
                RefreshStorage();
                Messages.Message("PRFReorganize_Message".Translate(), MessageTypeDefOf.NeutralEvent);
            },
            defaultLabel = "PRFReorganize".Translate(),
            defaultDesc = "PRFReorganize_Desc".Translate()
        };
    }

    public virtual string GetUIThingLabel() => "PRFMassStorageUIThingLabel".Translate(StoredItemsCount);
    public virtual string GetITabString(int itemsSelected) => "PRFItemsTabLabel".Translate(StoredItemsCount, itemsSelected);

    public virtual void RegisterNewItem(Thing newItem)
    {
        var things = Position.GetThingList(Map);
        for (var i = 0; i < things.Count; i++)
        {
            var item = things[i];
            if (item == newItem) continue;
            if (item.def.category == ThingCategory.Item && item.CanStackWith(newItem))
                item.TryAbsorbStack(newItem, true);
            if (newItem.Destroyed) break;
        }

        //Add a new stack of a thing
        if (!newItem.Destroyed)
        {
            if (!_items.Contains(newItem))
            {
                _items.Add(newItem);
                ItemCountsAdded(newItem.def, newItem.stackCount);
            }

            //What appens if its full?
            if (CanStoreMoreItems) newItem.Position = Position;
            if (!newItem.Spawned) newItem.SpawnSetup(Map, false);
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref _ports, "ports", LookMode.Reference);
        Scribe_Values.Look(ref _uniqueName, "uniqueName");
        ModExtensionCrate ??= def.GetModExtension<DefModExtension_Crate>();
    }

    public override string GetInspectString()
    {
        var original = base.GetInspectString();
        var stringBuilder = new StringBuilder();
        if (!string.IsNullOrEmpty(original)) stringBuilder.AppendLine(original);
        stringBuilder.Append("PRF_TotalStacksNum".Translate(_items.Count));
        return stringBuilder.ToString();
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        var thingsToSplurge = new List<Thing>(Position.GetThingList(Map));
        for (var i = 0; i < thingsToSplurge.Count; i++)
            if (thingsToSplurge[i].def.category == ThingCategory.Item)
            {
                thingsToSplurge[i].DeSpawn();
                GenPlace.TryPlaceThing(thingsToSplurge[i], Position, Map, ThingPlaceMode.Near);
            }

        foreach (var cell in this.OccupiedRect().Cells)
        {
            var component = Map.GetDsuComponent();
            component.HideItems.Remove(cell);
            component.HideRightMenus.Remove(cell);
        }

        base.DeSpawn(mode);
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        _outputUtil = new StorageOutputUtil(this);
        foreach (var cell in this.OccupiedRect().Cells)
        {
            var component = map.GetDsuComponent();
            component.HideItems.Add(cell);
            component.HideRightMenus.Add(cell);
        }

        ModExtensionCrate ??= def.GetModExtension<DefModExtension_Crate>();
        RefreshStorage();
        foreach (var port in _ports)
        {
            if (port?.Spawned ?? false)
            {
                if (port.Map != map)
                {
                    port.BoundStorageUnit = null;
                }
            }
        }
    }

    public override void DrawGUIOverlay()
    {
        base.DrawGUIOverlay();
        if (Current.CameraDriver.CurrentZoom <= CameraZoomRange.Close)
            GenMapUI.DrawThingLabel(this, LabelCap + "\n\r" + GetUIThingLabel());
    }

    public bool OutputItem(Thing item) => _outputUtil.OutputItem(item);

    //TODO Why do we need to clear Items here?
    public virtual void RefreshStorage()
    {
        _items.Clear();
        _itemCounts.Clear();

        if (!Spawned) return; // don't want to try getting lists of things when not on a map (see 155)
        foreach (var cell in AllSlotCells())
        {
            var things = new List<Thing>(cell.GetThingList(Map));
            for (var i = 0; i < things.Count; i++)
            {
                var item = things[i];
                if (item.def.category == ThingCategory.Item)
                {
                    if (cell != Position)
                    {
                        HandleNewItem(item);
                    }
                    else
                    {
                        if (!_items.Contains(item))
                        {
                            _items.Add(item);
                            ItemCountsAdded(item.def, item.stackCount);
                            DeregisterDrawItem(item);
                        }
                    }
                }
            }
        }
    }

    //-----------    For compatibility with Pick Up And Haul:    -----------
    //                  (not used internally in any way)
    // true if can store, capacity is how many can store (more than one stack possible)
    public bool CapacityAt(Thing thing, IntVec3 cell, Map map, out int capacity)
    {
        //Some Sanity Checks
        capacity = 0;
        if (thing == null || map == null || map != Map || cell == null || !Spawned)
        {
            Log.Error("PRF DSU CapacityAt Sanity Check Error");
            return false;
        }
        
        Log.Warning("--- CapacityAt called!");

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

    // ...The above? I think?  But without needing to know how many
    public bool StackableAt(Thing thing, IntVec3 cell, Map map)
    {
        return CapacityAt(thing, cell, map, out _);
    }

    public void HandleNewItem(Thing item)
    {
        RegisterNewItem(item);
        DeregisterDrawItem(item);
    }

    private void DeregisterDrawItem(Thing item) => Map.dynamicDrawManager.DeRegisterDrawable(item);

    public void HandleMoveItem(Thing item)
    {
        //throw new System.NotImplementedException();
    }

    public bool CanReciveThing(Thing item) => settings.AllowedToAccept(item) && CanReceiveIO && CanStoreMoreItems;

    public bool HoldsPos(IntVec3 pos) => AllSlotCells()?.Contains(pos) ?? false;

    private void ItemCountsRemoved(ThingDef thingDef, int cnt)
    {
        if (_itemCounts.TryGetValue(thingDef, out var count))
        {
            if (cnt > count)
            {
                Log.Error($"ItemCountsRemoved attempted to remove {cnt}/{count} Items of {thingDef}");
                _itemCounts[thingDef] = 0;
            }

            _itemCounts[thingDef] -= cnt;
        }
        else
        {
            Log.Error($"ItemCountsRemoved attempted to remove nonexistent def {thingDef}");
        }
    }

    private void ItemCountsAdded(ThingDef thingDef, int cnt)
    {
        if (!_itemCounts.TryAdd(thingDef, cnt))
        {
            _itemCounts[thingDef] += cnt;
        }
    }

    private readonly Dictionary<ThingDef, int> _itemCounts = new();

    public bool ItemIsLimit(ThingDef thing, bool cntStacks, int limit)
    {
        if (limit < 0) return true;

        _itemCounts.TryGetValue(thing, out var cnt);
        if (cntStacks)
        {
            cnt = Mathf.CeilToInt(((float)cnt) / thing.stackLimit);
        }

        return cnt >= limit;
    }
}