using System.Collections.Generic;
using System.Linq;
using DigitalStorageUnit.map;
using Verse;

namespace DigitalStorageUnit.util;

public static class AdvancedIOPatchHelper
{
    /// <summary>
    /// Gets all Ports that could be used
    /// They are Powerd, connected and the connected DSU is also powerd
    /// </summary>
    private static IEnumerable<KeyValuePair<IntVec3, Building_AdvancedStorageUnitIOPort>> GetAdvancedIOPorts(Map map) =>
        map.GetDsuComponent().AdvancedPortLocations.Where(l => (l.Value.boundStorageUnit?.Powered ?? false) && l.Value.CanReceiveNewItem);

    /// <summary>
    /// Orders IO Ports by Distance to an referencePos
    /// </summary>
    private static IEnumerable<KeyValuePair<float, Building_AdvancedStorageUnitIOPort>> GetOrderdAdvancedIOPorts(Map map, IntVec3 referencePos)
    {
        var dictIOports = GetAdvancedIOPorts(map);
        var ports = new List<KeyValuePair<float, Building_AdvancedStorageUnitIOPort>>();
        foreach (var pair in dictIOports)
        {
            var distance = pair.Key.DistanceTo(referencePos);
            ports.Add(new KeyValuePair<float, Building_AdvancedStorageUnitIOPort>(distance, pair.Value));
        }

        return ports.OrderBy(i => i.Key).ToList();
    }

    /// <summary>
    /// Orders IO Ports by Distance to an referencePos (pathfinding)
    /// </summary>
    private static List<KeyValuePair<float, Building_AdvancedStorageUnitIOPort>> GetOrderdAdvancedIOPorts(Map map, IntVec3 pawnPos, IntVec3 targetPos)
    {
        var dictIOports = GetAdvancedIOPorts(map);
        var ports = new List<KeyValuePair<float, Building_AdvancedStorageUnitIOPort>>();
        foreach (var pair in dictIOports)
        {
            var distance = GetTotalDistance(pawnPos, pair.Key, targetPos);
            ports.Add(new KeyValuePair<float, Building_AdvancedStorageUnitIOPort>(distance, pair.Value));
        }

        return ports.OrderBy(i => i.Key).ToList();
    }

    /// <summary>
    /// Returns The Closest Port
    /// </summary>
    public static KeyValuePair<float, Building_AdvancedStorageUnitIOPort> GetClosestPort(Map map, IntVec3 referencePos) =>
        GetOrderdAdvancedIOPorts(map, referencePos).FirstOrDefault();

    /// <summary>
    /// Returns the Closest Port that can transport a specific thing
    /// While being closer then a defined maxDistance
    /// </summary>
    public static KeyValuePair<float, Building_AdvancedStorageUnitIOPort> GetClosestPort(Map map, IntVec3 pawnPos, IntVec3 targetPos, Thing thing, float maxDistance) =>
        GetOrderdAdvancedIOPorts(map, pawnPos, targetPos).FirstOrDefault(p => p.Key < maxDistance && CanMoveItem(p.Value, thing));

    /// <summary>
    /// Checks if a Port can Move a specific Item
    /// </summary>
    public static bool CanMoveItem(Building_AdvancedStorageUnitIOPort port, Thing thing) => port.boundStorageUnit?.StoredItems?.Contains(thing) ?? false;

    /// <summary>
    /// Checks if a Port can Move a specific Item
    /// </summary>
    public static bool CanMoveItem(Building_AdvancedStorageUnitIOPort port, IntVec3 thingPos) => port.boundStorageUnit?.HoldsPos(thingPos) ?? false;

    /// <summary>
    /// Calculates the Full Path Cost
    /// But it can't see walls / Tarrain
    /// This is cheap
    /// 1 Call ~ 0.2us
    /// </summary>
    public static float GetTotalDistance(IntVec3 pawnPos, IntVec3 thingPos, IntVec3 targetPos) => pawnPos.DistanceTo(thingPos) + thingPos.DistanceTo(targetPos);

    /// <summary>
    /// Calculates the Full Path Cost
    /// Checking for walls and alike
    /// The issue with this is that it is extramly expencive.
    /// 1 Call ~ 0.4ms
    /// I Hope there is a better way to make this kind of a check
    /// maybe a manual calculation without the extra stepps included?
    /// </summary>
    public static float CalculatePathCost(Pawn pawn, IntVec3 thingPos, IntVec3 targetPos, Map map)
    {
        return map.pathFinder.FindPath(pawn.Position, thingPos, pawn).TotalCost + map.pathFinder.FindPath(thingPos, targetPos, pawn).TotalCost;
    }
}