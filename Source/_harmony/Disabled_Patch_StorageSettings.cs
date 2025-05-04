using DigitalStorageUnit.util;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DigitalStorageUnit._harmony;

// [HarmonyPatch(typeof(StorageSettings))]
public static class Disabled_Patch_StorageSettings
{
    // [HarmonyPatch(nameof(StorageSettings.AllowedToAccept), typeof(Thing))]
    // [HarmonyPrefix]
    // Patch_StoreUtility_TryFindBestBetterStoreCellForWorker
    public static bool Prefix(IStoreSettingsParent ___owner, Thing t, out bool __result)
    {
        __result = false;
        if (___owner is Building_Storage storage)
            //Check if pawn input is forbidden
            if ((storage as IForbidPawnInputItem)?.ForbidPawnInput ?? false)
                //#699 #678
                //This check is needed to support the use of the Limit function for the IO Ports
                if (storage.Position != t.Position)
                    return false;
        return true;
    }
}