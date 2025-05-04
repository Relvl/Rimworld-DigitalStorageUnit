using System.Collections.Generic;
using DigitalStorageUnit.map;
using Verse;

namespace DigitalStorageUnit.extensions;

public static class MapExtension
{
    private static readonly Dictionary<Map, DsuMapComponent> MapCompsCache = new();

    public static DsuMapComponent GetDsuComponent(this Map map)
    {
        DsuMapComponent result = null;
        if (map is not null && !MapCompsCache.TryGetValue(map, out result))
        {
            result = map.GetComponent<DsuMapComponent>();
            MapCompsCache.Add(map, result);
        }

        return result;
    }

    public static void OnMapRemoved(Map map)
    {
        MapCompsCache.Remove(map);
    }

    public static bool IsDSUOnPoint(this Map map, IntVec3 point)
    {
        return map.GetDsuComponent()?.IsDSUOnPoint(point) ?? false;
    }

    public static IEnumerable<DigitalStorageUnitBuilding> GetAllPoweredDSU(this Map map, bool any = false)
    {
        foreach (var building in map.listerBuildings.AllBuildingsColonistOfClass<DigitalStorageUnitBuilding>())
        {
            if (!building.CanWork) continue;
            yield return building;
            if (any) break;
        }
    }
}