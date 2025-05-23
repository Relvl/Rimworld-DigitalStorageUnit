using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using DigitalStorageUnit.extensions;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace DigitalStorageUnit._harmony;

/// <summary>
///     Allows bill to search intems through AccessPoint within ingredient search radius
/// </summary>
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[HarmonyPatch] // with no namings, we're using [HarmonyTargetMethod]
public static class Patch_WorkGiver_DoBill
{
    private static Type _hiddenClass;
    private static FieldInfo _regionsProcessed;
    private static FieldInfo _relevantThings;

    private static Predicate<Thing> _thingValidator;
    private static Pawn _pawn;

    private static readonly HashSet<DigitalStorageUnitBuilding> FoundDsuWithPortsInRadius = [];


    /// <summary>
    ///     We can't mix TargetMethod and manual patch. So... Inner class, yeah. That's better than two classes with shared vars.
    ///     This patch allows to provide items for bill from the DSU's not in the radius, for Access Points in the radius.
    /// </summary>
    [HarmonyPatch(typeof(WorkGiver_DoBill))]
    public static class InnerPatch_AddItemsFromFoundDsu
    {
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            // we need to find out an inner class, near the line like this:
            // "RegionProcessor regionProcessor = (RegionProcessor) (r =>" around line 380 of the class.
            // Look at similar name around there.
            _hiddenClass = typeof(WorkGiver_DoBill).GetNestedTypes(AccessTools.all).FirstOrDefault(t => t.FullName!.Contains("c__DisplayClass24_0"));
            if (_hiddenClass is null)
            {
                Log.Error("DSU: Can't find WorkGiver_DoBill.c__DisplayClass24_0 class");
                return null;
            }

            _regionsProcessed = _hiddenClass.GetFields(AccessTools.all).FirstOrDefault(t => t.Name.EndsWith("regionsProcessed"));

            // the orinignal IL is like this:
            //     IL_01fd: ldloc.0      // V_0
            // IL_01fe: ldftn        instance bool RimWorld.WorkGiver_DoBill/'<>c__DisplayClass24_0'::'<TryFindBestIngredientsHelper>b__4'(class Verse.Region)
            // IL_0204: newobj       instance void Verse.RegionProcessor::.ctor(object, native int)
            // IL_0209: stloc.3      // regionProcessor
            // And we need to find a name for that method in the IL, loaded by "ldftn", and determine it somehow, mostly just by part of name
            var methodInfo = _hiddenClass.GetMethods(AccessTools.all).FirstOrDefault(t => t.Name.Contains("b__4"));
            if (methodInfo is null)
            {
                Log.Error("DSU: Can't find WorkGiver_DoBill.<>c__DisplayClass24_0.<TryFindBestIngredientsHelper>b__4 method");
                return null;
            }

            return methodInfo;
        }

        /// <summary>
        ///     Method like about 380 line of WorkGiver_DoBill.TryFindBestIngredientsHelper
        ///     There is inner synthetic class like "RegionProcessor regionProcessor = (RegionProcessor) (r =>" we're needed to inject into it.
        ///     WorkGiver_DoBill.relevantThings.AddRange((IEnumerable Thing ) WorkGiver_DoBill.newRelevantThings); // We need to inject around/before this line
        ///     This thing looks around, collecting matching ingredients. If we've found an Access Point into the radius - we want to provide an item there.
        /// </summary>
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var regionsProcessedFound = false;
            var patched = false;

            foreach (var instruction in instructions)
            {
                // ++regionsProcessed;  // <----- this one is found, we need to inject after it, before "> 0" comparsion
                // if (WorkGiver_DoBill.newRelevantThings.Count > 0 && regionsProcessed > adjacentRegionsAvailable)
                if (!regionsProcessedFound && instruction.opcode == OpCodes.Stfld && instruction.operand.ToString() == "System.Int32 regionsProcessed")
                {
                    regionsProcessedFound = true;
                    yield return instruction;
                    continue;
                }

                // just next to it:
                // ++regionsProcessed;
                // -----------> {inject here} <------------
                // if (WorkGiver_DoBill.newRelevantThings.Count > 0 && regionsProcessed > adjacentRegionsAvailable)
                if (regionsProcessedFound && !patched)
                {
                    // ldarg.0      this
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    // ldfld        int32 RimWorld.WorkGiver_DoBill/'<>c__DisplayClass24_0'::regionsProcessed
                    yield return new CodeInstruction(OpCodes.Ldfld, _regionsProcessed);
                    // ldsfld       class [mscorlib]System.Collections.Generic.List`1<class Verse.Thing> RimWorld.WorkGiver_DoBill::newRelevantThings
                    yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(WorkGiver_DoBill), "newRelevantThings"));
                    // add our call to Patch_WorkGiver_DoBill.ProcessRegion(regionsProcessed, relevantThings)
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(InnerPatch_AddItemsFromFoundDsu), nameof(Injected_AddThingsFromDSU)));

                    Log.Message("DSU: Patch_WorkGiver_DoBill_DisplayClass_24_0_HiddenMethod OK");

                    patched = true;
                    yield return instruction;
                    continue;
                }

                yield return instruction;
            }
        }

        /// <summary>
        ///     Called by instrumentation above
        /// </summary>
        public static void Injected_AddThingsFromDSU(int regionsProcessed, List<Thing> relevantThings)
        {
            if (!DigitalStorageUnit.Config.BillSearchRadiusFix) return;
            if (_pawn is null || _thingValidator is null) return;

            // https://discord.com/channels/272340793174392832/439514175245516800/1161722483502760016
            // Well... The vanilla will traverse ALL the region if not found in radius... So...
            if (regionsProcessed != 1) return; // 1 is cuz it's increments before injected call. No any 0's there.

            // Afterall, these items will be provided to port if needed in Patch_Pawn_JobTracker

            foreach (var dsu in FoundDsuWithPortsInRadius)
            foreach (var item in dsu.GetStoredThings())
            {
                if (relevantThings.Contains(item)) continue;
                if (item.IsForbidden(_pawn)) continue;
                if (!_pawn.CanReserve(item)) continue;
                if (!_thingValidator(item)) continue;
                relevantThings.Add(item);
            }
        }
    }


    /// <summary>
    ///     We can't mix TargetMethod and manual patch. So... Inner class, yeah. That's better than two classes with shared vars.
    ///     This patch looks for DSU's that has Access Points in the bill radius, and after a search calls finalizing things.
    /// </summary>
    [HarmonyPatch(typeof(WorkGiver_DoBill))]
    public static class InnerPatch_TryFindBestIngredientsHelper
    {
        /// <summary>
        ///     Before it's called we should collect and cache all the DSU's with Access Points in the bill range.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch("TryFindBestIngredientsHelper")]
        public static void TryFindBestIngredientsHelper_Prefix(Predicate<Thing> thingValidator, Pawn pawn, Thing billGiver, float searchRadius)
        {
            _thingValidator = thingValidator;
            _pawn = pawn;
            if (billGiver is null || !DigitalStorageUnit.Config.BillSearchRadiusFix) return;
            var radiusSq = searchRadius * searchRadius;
            FoundDsuWithPortsInRadius.AddRange(
                billGiver.Map.listerBuildings.AllBuildingsColonistOfClass<AccessPointPortBuilding>()
                    .Where(
                        p => p.Spawned &&
                             p.Powered &&
                             p.BoundStorageUnit is not null &&
                             p.BoundStorageUnit.CanWork &&
                             p.Position.DistanceToSquared(billGiver.Position) <= radiusSq
                    )
                    .Select(p => p.BoundStorageUnit)
            );
        }

        /// <summary>
        ///     Cleanup saved things
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("TryFindBestIngredientsHelper")]
        public static void TryFindBestIngredientsHelper_Postfix()
        {
            _thingValidator = null;
            _pawn = null;
            FoundDsuWithPortsInRadius.Clear();
        }
    }

    /// <summary>
    ///     We can't mix TargetMethod and manual patch. So... Inner class, yeah. That's better than two classes with shared vars.
    /// </summary>
    [HarmonyPatch(typeof(WorkGiver_DoBill))]
    public static class InnerPatch_TryStartNewDoBillJob
    {
        /// <summary>
        ///     Split the item stacks when DoBill job collects the ingredients.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(nameof(WorkGiver_DoBill.TryStartNewDoBillJob))]
        public static void Postfix(ref Job __result)
        {
            if (__result?.targetQueueB is null) return;
            var component = __result.targetA.Thing?.Map?.GetDsuComponent();
            if (component is null) return;

            for (var idx = 0; idx < __result.targetQueueB.Count; idx++)
            {
                var info = __result.targetQueueB[idx];
                if (!info.HasThing) continue;
                var dsu = component.GetDsuHoldingItem(info.Thing);
                if (dsu is null) return;
                var count = __result.countQueue[idx];

                if (count > 0 && count < info.Thing.stackCount && dsu.CanWork)
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
}