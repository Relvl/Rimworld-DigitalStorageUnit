using HarmonyLib;
using System.Linq;
using DigitalStorageUnit.Storage;
using Verse;
using Verse.AI;

namespace DigitalStorageUnit.Common.HarmonyPatches;

[HarmonyPatch(typeof(ReservationManager), "Reserve")]
class Patch_Reservation_Reservation_IO
{
    static bool Prefix(Pawn claimant, Job job, LocalTargetInfo target, ref bool __result, Map ___map)
    {
        if (target.HasThing == false && ___map != null && target.Cell.InBounds(___map))
        {
            var building_target = (Building_StorageUnitIOBase)target.Cell.GetThingList(___map).Where(t => t is Building_StorageUnitIOBase).FirstOrDefault();
            if (building_target != null && building_target.mode == StorageIOMode.Input)
            {
                __result = true;
                return false;
            }
        }

        return true;
    }
}