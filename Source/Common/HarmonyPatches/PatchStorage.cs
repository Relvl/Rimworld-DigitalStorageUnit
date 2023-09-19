using RimWorld;
using System.Collections.Generic;
using Verse;

namespace DigitalStorageUnit.Common.HarmonyPatches;

class Patch_ForbidUtility_IsForbidden
{
    static bool Prefix(Thing t, Pawn pawn, out bool __result)
    {
        __result = true;
        if (t != null)
        {
            var thingmap = t.Map;
            if (thingmap != null && t.def.category == ThingCategory.Item)
            {
                if (PatchStorageUtil.GetPRFMapComponent(thingmap)?.ShouldForbidPawnOutputAtPos(t.Position) ?? false)
                {
                    return false;
                }
            }
        }

        return true;
    }
}

class Patch_Building_Storage_Accepts
{
    static bool Prefix(Building_Storage __instance, Thing t, out bool __result)
    {
        __result = false;
        //Check if pawn input is forbidden
        if ((__instance as IForbidPawnInputItem)?.ForbidPawnInput ?? false)
        {
            //#699 #678
            //This check is needed to support the use of the Limit function for the IO Ports
            if (__instance.Position != t.Position)
            {
                return false;
            }
        }

        return true;
    }
}

static class PatchStorageUtil
{
    private static Dictionary<Map, PRFMapComponent> mapComps = new();

    public static PRFMapComponent GetPRFMapComponent(Map map)
    {
        PRFMapComponent outval = null;
        if (map is not null && !mapComps.TryGetValue(map, out outval))
        {
            outval = map.GetComponent<PRFMapComponent>();
            mapComps.Add(map, outval);
        }

        return outval;
    }
}

public interface IHideItem
{
    bool HideItems { get; }
}

public interface IForbidPawnOutputItem
{
    bool ForbidPawnOutput { get; }
}

public interface IForbidPawnInputItem : ISlotGroupParent, IHaulDestination
{
    bool ForbidPawnInput { get; }
}