namespace DigitalStorageUnit.Common.HarmonyPatches;

/// <summary>
/// Patch to ensure Items in Cold Storage Contribute to Wealth
/// 1k Items ~ 1ms (every 5k Ticks)
/// </summary>
class Patch_WealthWatcher_CalculateWealthItems
{
    public static void Postfix(Verse.Map ___map, ref float __result)
    {
        foreach (var (pos, storage) in ___map.GetDsuComponent().ColdStorageLocations)
        {
            __result += storage.GetItemWealth();
        }
    }
}