﻿using System.Collections.Generic;
using Verse;

namespace ProjectRimFactory.Common;

public class PRFMapComponent : MapComponent
{
    private List<ITicker> tickers = new();

    // iHideRightMenus: see HarmonyPatches/PatchStorage.cs
    public HashSet<IntVec3> iHideRightMenus = new();

    public List<Storage.Building_ColdStorage> ColdStorageBuildings = new();

    private Dictionary<IntVec3, List<HarmonyPatches.IHideItem>> hideItemLocations = new();

    private Dictionary<IntVec3, List<HarmonyPatches.IForbidPawnOutputItem>> ForbidPawnOutputItemLocations = new();

    private Dictionary<IntVec3, Storage.Building_AdvancedStorageUnitIOPort> advancedIOLocations = new();

    public Dictionary<IntVec3, Storage.Building_AdvancedStorageUnitIOPort> GetadvancedIOLocations => advancedIOLocations;


    public void RegisterColdStorageBuilding(Storage.Building_ColdStorage port)
    {
        if (!ColdStorageBuildings.Contains(port))
        {
            ColdStorageBuildings.Add(port);
        }
    }

    public void DeRegisterColdStorageBuilding(Storage.Building_ColdStorage port)
    {
        if (ColdStorageBuildings.Contains(port))
        {
            ColdStorageBuildings.Remove(port);
        }
    }

    public void RegisteradvancedIOLocations(IntVec3 pos, Storage.Building_AdvancedStorageUnitIOPort port)
    {
        if (!advancedIOLocations.ContainsKey(pos))
        {
            advancedIOLocations.Add(pos, port);
        }
    }

    public void DeRegisteradvancedIOLocations(IntVec3 pos)
    {
        if (advancedIOLocations.ContainsKey(pos))
        {
            advancedIOLocations.Remove(pos);
        }
    }

    public void RegisterIHideItemPos(IntVec3 pos, HarmonyPatches.IHideItem hideItem)
    {
        if (hideItemLocations.ContainsKey(pos))
        {
            hideItemLocations[pos].Add(hideItem);
        }
        else
        {
            hideItemLocations.Add(pos, new List<HarmonyPatches.IHideItem>() { hideItem });
        }
    }

    public void DeRegisterIHideItemPos(IntVec3 pos, HarmonyPatches.IHideItem hideItem)
    {
        if (hideItemLocations[pos].Count <= 1)
        {
            hideItemLocations.Remove(pos);
        }
        else
        {
            hideItemLocations[pos].Remove(hideItem);
        }
    }

    public void RegisterIForbidPawnOutputItem(IntVec3 pos, HarmonyPatches.IForbidPawnOutputItem ForbidPawnOutput)
    {
        if (ForbidPawnOutputItemLocations.ContainsKey(pos))
        {
            ForbidPawnOutputItemLocations[pos].Add(ForbidPawnOutput);
        }
        else
        {
            ForbidPawnOutputItemLocations.Add(pos, new List<HarmonyPatches.IForbidPawnOutputItem>() { ForbidPawnOutput });
        }
    }

    public void DeRegisterIForbidPawnOutputItem(IntVec3 pos, HarmonyPatches.IForbidPawnOutputItem ForbidPawnOutput)
    {
        if (ForbidPawnOutputItemLocations[pos].Count <= 1)
        {
            ForbidPawnOutputItemLocations.Remove(pos);
        }
        else
        {
            ForbidPawnOutputItemLocations[pos].Remove(ForbidPawnOutput);
        }
    }

    public List<HarmonyPatches.IHideItem> CheckIHideItemPos(IntVec3 pos)
    {
        if (hideItemLocations.ContainsKey(pos)) return hideItemLocations[pos];
        return null;
    }

    public bool ShouldHideItemsAtPos(IntVec3 pos)
    {
        return CheckIHideItemPos(pos)?.Any(t => t.HideItems) ?? false;
    }

    public List<HarmonyPatches.IForbidPawnOutputItem> CheckIForbidPawnOutputItem(IntVec3 pos)
    {
        if (ForbidPawnOutputItemLocations.ContainsKey(pos)) return ForbidPawnOutputItemLocations[pos];
        return null;
    }

    public bool ShouldForbidPawnOutputAtPos(IntVec3 pos)
    {
        return CheckIForbidPawnOutputItem(pos)?.Any(t => t.ForbidPawnOutput) ?? false;
    }

    public PRFMapComponent(Map map) : base(map)
    {
    }

    public override void MapComponentTick()
    {
        base.MapComponentTick();
        tickers.ForEach(t => t.Tick());
    }

    public void AddTicker(ITicker ticker)
    {
        tickers.Add(ticker);
    }

    public void RemoveTicker(ITicker ticker)
    {
        tickers.Remove(ticker);
    }

    public void AddIHideRightClickMenu(Building b)
    {
        foreach (var v in b.OccupiedRect())
        {
            iHideRightMenus.Add(v);
        }
    }

    public void RemoveIHideRightClickMenu(Building b)
    {
        foreach (var v in b.OccupiedRect())
        {
            iHideRightMenus.Remove(v);
            foreach (var t in map.thingGrid.ThingsListAt(v))
            {
                if (t != b && t is IHideRightClickMenu) iHideRightMenus.Add(v);
            }
        }
    }
}

public interface ITicker
{
    void Tick();
}

// NOTE: You might register AND deregister via Map.GetComponent<PRFMapComponent>().iHideRightMenus.Add(this);
public interface IHideRightClickMenu
{
    bool HideRightClickMenus { get; }
}