using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace DigitalStorageUnit._harmony;

/// <summary>
/// Allows bill to search intems through AccessPoint within ingredient search radius
/// </summary>
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[HarmonyPatch]
public static class Patch_WorkGiver_DoBill_DisplayClass_24_0_HiddenMethod
{
    private static Type _hiddenClass;
    private static FieldInfo _regionsProcessed;
    private static FieldInfo _adjacentRegionsAvailable;
    private static FieldInfo _relevantThings;

    private static Predicate<Thing> _thingValidator;
    private static Pawn _pawn;

    private static readonly HashSet<DigitalStorageUnitBuilding> LinkedDsu = new();

    /// <summary>
    /// We can't mix TargetMethod and manual patch. So... Inner class, yeah.
    /// </summary>
    [HarmonyPatch]
    public static class InnerPrefix
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorkGiver_DoBill), "TryFindBestIngredientsHelper")]
        public static void TryFindBestIngredientsHelper_Prefix(Predicate<Thing> thingValidator, Pawn pawn, Thing billGiver, float searchRadius)
        {
            LinkedDsu.Clear();

            _thingValidator = thingValidator;
            _pawn = pawn;

            if (billGiver is not null && DigitalStorageUnit.Config.BillSearchRadiusFix)
            {
                LinkedDsu.AddRange(
                    billGiver.Map.listerBuildings.AllBuildingsColonistOfClass<AccessPointPortBuilding>()
                        .Where(p => p.Spawned && p.Powered && p.BoundStorageUnit is not null)
                        .Where(p => p.Position.DistanceToSquared(billGiver.Position) <= searchRadius * searchRadius)
                        .Select(p => p.BoundStorageUnit)
                );
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WorkGiver_DoBill), "TryFindBestIngredientsHelper")]
        public static void TryFindBestIngredientsHelper_Postfix(Predicate<List<Thing>> foundAllIngredientsAndChoose, bool __result)
        {
            if (!__result && DigitalStorageUnit.Config.WorkGiverDoBillUnnecessaryFix)
            {
                // Todo! chech if it was not called at all
                _relevantThings ??= AccessTools.Field(typeof(WorkGiver_DoBill), "relevantThings");
                foundAllIngredientsAndChoose(_relevantThings.GetValue(null) as List<Thing>);
            }

            _thingValidator = null;
            _pawn = null;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(WorkGiver_DoBill), "TryFindBestIngredientsHelper")]
        public static IEnumerable<CodeInstruction> TryFindBestIngredientsHelper_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var unnecessaryFoundAllIngredientsAndChooseIndex = -1;
            foreach (var instruction in instructions)
            {
                if (DigitalStorageUnit.Config.WorkGiverDoBillUnnecessaryFix)
                {
                    if (unnecessaryFoundAllIngredientsAndChooseIndex == -1 &&
                        instruction.opcode == OpCodes.Call &&
                        instruction.operand.ToString().StartsWith("Void AddRange[Thing]"))
                    {
                        unnecessaryFoundAllIngredientsAndChooseIndex = 0;
                        yield return instruction;
                        continue;
                    }

                    // Remove 364'th line:  int num = foundAllIngredientsAndChoose(WorkGiver_DoBill.relevantThings) ? 1 : 0;
                    if (unnecessaryFoundAllIngredientsAndChooseIndex is >= 0 and <= 4)
                    {
                        unnecessaryFoundAllIngredientsAndChooseIndex++;
                        continue;
                    }
                }

                yield return instruction;
            }
        }
    }

    /// <summary>
    /// Method like about 380 line of WorkGiver_DoBill
    /// WorkGiver_DoBill.relevantThings.AddRange((IEnumerable Thing ) WorkGiver_DoBill.newRelevantThings); // We need to inject after this line 
    /// </summary>
    public static MethodBase TargetMethod()
    {
        _hiddenClass = typeof(WorkGiver_DoBill).GetNestedTypes(AccessTools.all).FirstOrDefault(t => t.FullName!.Contains("c__DisplayClass24_0"));
        if (_hiddenClass is null)
        {
            Log.Error("DSU: Can't find WorkGiver_DoBill.c__DisplayClass24_0 class");
            return null;
        }

        _regionsProcessed = _hiddenClass.GetFields(AccessTools.all).FirstOrDefault(t => t.Name.EndsWith("regionsProcessed"));
        _adjacentRegionsAvailable = _hiddenClass.GetFields(AccessTools.all).FirstOrDefault(t => t.Name.EndsWith("adjacentRegionsAvailable"));

        var methodInfo = _hiddenClass.GetMethods(AccessTools.all).FirstOrDefault(t => t.Name.Contains("b__4"));
        if (methodInfo is null)
        {
            Log.Error("DSU: Can't find WorkGiver_DoBill.<>c__DisplayClass24_0.<TryFindBestIngredientsHelper>b__4 method");
            return null;
        }

        return methodInfo;
    }

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var regionsProcessedFound = false;
        var portSearchAdded = false;

        foreach (var instruction in instructions)
        {
            if (!regionsProcessedFound && instruction.opcode == OpCodes.Stfld && instruction.operand.ToString() == "System.Int32 regionsProcessed")
            {
                regionsProcessedFound = true;
                yield return instruction;
                continue;
            }

            if (regionsProcessedFound && !portSearchAdded)
            {
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldfld, _regionsProcessed);
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldfld, _adjacentRegionsAvailable);
                yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(WorkGiver_DoBill), "newRelevantThings"));
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_WorkGiver_DoBill_DisplayClass_24_0_HiddenMethod), nameof(ProcessRegion)));

                portSearchAdded = true;
                yield return instruction;
                continue;
            }

            yield return instruction;
        }
    }

    public static void ProcessRegion(int regionsProcessed, int adjacentRegionsAvailable, List<Thing> relevantThings)
    {
        if (!DigitalStorageUnit.Config.BillSearchRadiusFix) return;
        if (_pawn is null || _thingValidator is null) return;
        if (regionsProcessed <= adjacentRegionsAvailable) return;
        foreach (var dsu in LinkedDsu)
        {
            foreach (var item in dsu.StoredItems)
            {
                if (!item.Spawned) continue;
                if (item.IsForbidden(_pawn)) continue;
                if (!_pawn.CanReserve(item)) continue;
                if (!_thingValidator(item)) continue;
                if (relevantThings.Contains(item)) continue;
                relevantThings.Add(item);
            }
        }
    }
}