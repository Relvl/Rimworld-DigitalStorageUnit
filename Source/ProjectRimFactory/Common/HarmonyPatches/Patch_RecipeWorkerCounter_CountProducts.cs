using HarmonyLib;
using ProjectRimFactory.Storage;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
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
                var i = 0;
                var targetDef = __instance.recipe.products[0].thingDef;

                //Add Items form AssemblerQueue
                var gamecomp = Current.Game.GetComponent<PRFGameComponent>();
                for (i = 0; i < gamecomp.AssemblerQueue.Count; i++)
                {
                    //Don't count Resources of other maps
                    if (billmap != gamecomp.AssemblerQueue[i].Map) continue;
                    foreach (var heldThing in gamecomp.AssemblerQueue[i].GetThingQueue())
                    {
                        TryUpdateResult(ref __result, targetDef, heldThing);
                    }
                }

                //Add Items stored in ColdStorage
                var units = PatchStorageUtil.GetPRFMapComponent(billmap).ColdStorageBuildings.Select(b => b as ILinkableStorageParent).ToList();

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
}