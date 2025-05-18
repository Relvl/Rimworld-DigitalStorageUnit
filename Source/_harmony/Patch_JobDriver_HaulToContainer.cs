using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace DigitalStorageUnit._harmony;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[HarmonyPatch(typeof(JobDriver_HaulToContainer))]
public static class Patch_JobDriver_HaulToContainer
{
    [HarmonyPostfix]
    [HarmonyPatch("MakeNewToils")]
    public static void MakeNewToils(JobDriver_HaulToContainer __instance, ref IEnumerable<Toil> __result)
    {
        var toilList = new List<Toil>();
        foreach (var toil in __result)
        {
            if (toil.debugName == nameof(Toils_Goto.GotoThing))
            {
                var notifyDsu = Toils_General.Do(() =>
                {
                    var thingToHaul = __instance.job.GetTarget(TargetIndex.A).Thing;
                    var containerThing = __instance.job.GetTarget(TargetIndex.B).Thing;

                    var reachabilityResult = Patch_Reachability.CanReachAndFindAccessPoint(
                        __instance.pawn,
                        thingToHaul,
                        containerThing.Position,
                        PathEndMode.ClosestTouch,
                        TraverseParms.For(__instance.pawn)
                    );

                    if (reachabilityResult.Dsu is not null && reachabilityResult.AccessPoint is not null)
                        reachabilityResult.AccessPoint.ProvideItem(__instance.job.targetA.Thing);
                });
                notifyDsu.debugName = $"DSU: {nameof(MakeNewToils)}";
                toilList.Add(notifyDsu);
            }

            toilList.Add(toil);
        }

        __result = toilList;
    }
}