using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Verse;

namespace DigitalStorageUnit.Common;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")] // All MapComponents are initiated when the colony map is created. The tick starts with the first world tick.
public class DsuMapComponent : MapComponent
{
    public readonly HashSet<IntVec3> HideRightMenus = new();
    public readonly HashSet<IntVec3> HideItems = new();

    public Dictionary<IntVec3, Storage.Building_AdvancedStorageUnitIOPort> AdvancedPortLocations { get; } = new();
    public Dictionary<IntVec3, Storage.Building_ColdStorage> ColdStorageLocations { get; } = new();

    public DsuMapComponent(Map map) : base(map)
    {
    }

    public override void MapRemoved() => MapExtension.OnMapRemoved(map);
}

public static class MapExtension
{
    private static readonly Dictionary<Map, DsuMapComponent> MapCompsCache = new();

    public static DsuMapComponent GetDsuComponent(this Map map)
    {
        DsuMapComponent outval = null;
        if (map is not null && !MapCompsCache.TryGetValue(map, out outval))
        {
            outval = map.GetComponent<DsuMapComponent>();
            MapCompsCache.Add(map, outval);
        }

        return outval;
    }

    public static void OnMapRemoved(Map map) => MapCompsCache.Remove(map);
}