using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.util;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace DigitalStorageUnit._harmony;

/// <summary>
/// Split the item stacks when DoBill job collects the ingredients.
/// </summary>
[HarmonyPatch(typeof(RimWorld.WorkGiver_DoBill), nameof(RimWorld.WorkGiver_DoBill.TryStartNewDoBillJob))]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class Patch_WorkGiver_DoBill_TryStartNewDoBillJob
{
    public static void Postfix(ref Job __result)
    {
        var component = __result.targetA.Thing?.Map?.GetDsuComponent();
        if (component is null) return;

        for (var idx = 0; idx < __result.targetQueueB.Count; idx++)
        {
            var info = __result.targetQueueB[idx];
            if (!info.HasThing) continue;
            var dsu = component.GetDsuHoldingItem(info.Thing);
            if (dsu is null) return;
            var count = __result.countQueue[idx];

            if (count > 0 && count < info.Thing.stackCount && dsu.Powered)
            {
                // SplitOff without merging. Possible to overflow the DSU for a 1 tick. Don't care.
                var splitedThing = ThingMaker.MakeThing(info.Thing.def, info.Thing.Stuff);
                splitedThing.stackCount = info.Thing.stackCount - count;
                splitedThing.Position = info.Thing.Position;
                info.Thing.stackCount = count;
                info.Thing.DirtyMapMesh(info.Thing.Map);
                if (info.Thing.def.useHitPoints) splitedThing.HitPoints = info.Thing.HitPoints;
                splitedThing.SpawnSetup(info.Thing.Map, false);
                dsu.HandleNewItem(splitedThing, false);
            }
        }
    }
}