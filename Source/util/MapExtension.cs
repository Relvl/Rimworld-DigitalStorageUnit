using System.Collections.Generic;
using DigitalStorageUnit.map;
using Verse;

namespace DigitalStorageUnit.util;

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