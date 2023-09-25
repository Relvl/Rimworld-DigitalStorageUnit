using Verse;

namespace DigitalStorageUnit.util;

public static class IntVec3Extensions
{
    public static T GetFirst<T>(this IntVec3 c, Map map) where T : class
    {
        if (map == null || !c.InBounds(map)) return null;
        foreach (var th in map.thingGrid.ThingsListAt(c))
            if (th is T t)
                return t;
        return null;
    }
}