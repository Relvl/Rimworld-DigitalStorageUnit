using HarmonyLib;
using RimWorld;
using System.Linq;
using DigitalStorageUnit.Storage;
using Verse;

namespace DigitalStorageUnit.Common.HarmonyPatches;

/// <summary>
/// This Patch Counts additional Items for the Do until X Type Bills
/// Currently adds Items from:
/// - AssemblerQueue
/// - Cold STorage
/// 
/// Old Note: Art & maybe other things too need a separate patch
/// </summary>
[HarmonyPatch(typeof(RecipeWorkerCounter), "CountProducts")]
class Patch_RecipeWorkerCounter_CountProducts
{
    static void Postfix(RecipeWorkerCounter __instance, ref int __result, Bill_Production bill)
    {
        //Only run if Check everywhere is set
        if (bill.includeFromZone == null)
        {
            var billmap = bill.Map;
            var targetDef = __instance.recipe.products[0].thingDef;

            //Add Items stored in ColdStorage
            var units = billmap.GetDsuComponent().ColdStorageLocations.Values.Select(b => b as ILinkableStorageParent).ToList();

            foreach (var dsu in units)
            {
                foreach (var thing in dsu.StoredItems)
                {
                    TryUpdateResult(ref __result, targetDef, thing);
                }
            }
        }
    }

    private static void TryUpdateResult(ref int __result, ThingDef targetDef, Thing heldThing)
    {
        var innerIfMinified = heldThing.GetInnerIfMinified();
        if (innerIfMinified.def == targetDef)
        {
            __result += innerIfMinified.stackCount;
        }
    }
}