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

    private static readonly HashSet<DigitalStorageUnitBuilding> LinkedDsu = new();

    /// <summary>
    ///     Method like about 380 line of WorkGiver_DoBill
    ///     WorkGiver_DoBill.relevantThings.AddRange((IEnumerable Thing ) WorkGiver_DoBill.newRelevantThings); // We need to inject after this line
    /// </summary>
    [HarmonyTargetMethod]
    public static MethodBase TargetMethod()
    {
        _hiddenClass = typeof(WorkGiver_DoBill).GetNestedTypes(AccessTools.all).FirstOrDefault(t => t.FullName!.Contains("c__DisplayClass24_0"));
        if (_hiddenClass is null)
        {
            Log.Error("DSU: Can't find WorkGiver_DoBill.c__DisplayClass24_0 class");
            return null;
        }

        _regionsProcessed = _hiddenClass.GetFields(AccessTools.all).FirstOrDefault(t => t.Name.EndsWith("regionsProcessed"));

        var methodInfo = _hiddenClass.GetMethods(AccessTools.all).FirstOrDefault(t => t.Name.Contains("b__4"));
        if (methodInfo is null)
        {
            Log.Error("DSU: Can't find WorkGiver_DoBill.<>c__DisplayClass24_0.<TryFindBestIngredientsHelper>b__4 method");
            return null;
        }

        return methodInfo;
    }

    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var regionsProcessedFound = false;
        var patched = false;

        foreach (var instruction in instructions)
        {
            if (!regionsProcessedFound && instruction.opcode == OpCodes.Stfld && instruction.operand.ToString() == "System.Int32 regionsProcessed")
            {
                regionsProcessedFound = true;
                yield return instruction;
                continue;
            }

            if (regionsProcessedFound && !patched)
            {
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldfld, _regionsProcessed);
                yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(WorkGiver_DoBill), "newRelevantThings"));
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_WorkGiver_DoBill), nameof(ProcessRegion)));

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
    public static void ProcessRegion(int regionsProcessed, List<Thing> relevantThings)
    {
        if (!DigitalStorageUnit.Config.BillSearchRadiusFix) return;
        if (_pawn is null || _thingValidator is null) return;

        // https://discord.com/channels/272340793174392832/439514175245516800/1161722483502760016
        // Well... The vanilla will traverse ALL the region if not found in radius... So...
        if (regionsProcessed != 1) return; // 1 is cuz it's increments first. No any 0 there.

        foreach (var dsu in LinkedDsu)
        foreach (var item in dsu.GetStoredThings())
        {
            if (relevantThings.Contains(item)) continue;
            if (item.IsForbidden(_pawn)) continue;
            if (!_pawn.CanReserve(item)) continue;
            if (!_thingValidator(item)) continue;
            relevantThings.Add(item);
        }
    }

    /// <summary>
    ///     We can't mix TargetMethod and manual patch. So... Inner class, yeah. That's better than two classes with shared vars.
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
            LinkedDsu.Clear();
            _thingValidator = thingValidator;
            _pawn = pawn;
            if (billGiver is not null && DigitalStorageUnit.Config.BillSearchRadiusFix)
            {
                var radiusSq = searchRadius * searchRadius;
                LinkedDsu.AddRange(
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
        }

        /// <summary>
        ///     Prevents premature call of foundAllIngredientsAndChoose()
        ///     We need to collect items from all found DSU's before choose the ingredients.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch("TryFindBestIngredientsHelper")]
        public static IEnumerable<CodeInstruction> TryFindBestIngredientsHelper_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var unnecessaryFoundAllIngredientsAndChooseIndex = -1;
            foreach (var instruction in instructions)
            {
                if (DigitalStorageUnit.Config.WorkGiverDoBillUnnecessaryFix)
                {
                    if (unnecessaryFoundAllIngredientsAndChooseIndex == -1 && instruction.opcode == OpCodes.Call && instruction.operand.ToString().StartsWith("Void AddRange[Thing]"))
                    {
                        unnecessaryFoundAllIngredientsAndChooseIndex = 0;
                        yield return instruction;
                        continue;
                    }

                    // Remove 373'th line:  int num = foundAllIngredientsAndChoose(WorkGiver_DoBill.relevantThings) ? 1 : 0;
                    if (unnecessaryFoundAllIngredientsAndChooseIndex is >= 0 and <= 4)
                    {
                        unnecessaryFoundAllIngredientsAndChooseIndex++;
                        continue;
                    }
                }

                yield return instruction;
            }
        }

        /// <summary>
        ///     And now, when we collected all the items, we can choose the ingredient finally, calling prevented before foundAllIngredientsAndChoose()
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch("TryFindBestIngredientsHelper")]
        public static void TryFindBestIngredientsHelper_Postfix(Predicate<List<Thing>> foundAllIngredientsAndChoose, bool __result)
        {
            // todo! really want to check for found result?..
            if (!__result && DigitalStorageUnit.Config.WorkGiverDoBillUnnecessaryFix)
            {
                // Todo! chech if it was not called at all
                // todo! do Harmony allows to access of private fields via "List<Thing> __relevantThings" ?
                _relevantThings ??= AccessTools.Field(typeof(WorkGiver_DoBill), "relevantThings");
                foundAllIngredientsAndChoose(_relevantThings.GetValue(null) as List<Thing>);
            }

            _thingValidator = null;
            _pawn = null;
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