using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace DigitalStorageUnit._harmony;

[HarmonyPatch]
public class Patch_WorkGiver_DoBill_DisplayClass_24_0_HiddenMethod
{
    private static Type _hiddenClass;
    private static FieldInfo _regionsProcessed;
    private static FieldInfo _adjacentRegionsAvailable;

    private static Predicate<Thing> _thingValidator;
    private static Pawn _pawn;
    private static readonly HashSet<DigitalStorageUnitBuilding> _linkedDsu = new();

    /// <summary>
    /// We can't mix TargetMethod and manual patch. So... Inner class, yeah.
    /// </summary>
    [HarmonyPatch]
    public static class InnerPrefix
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorkGiver_DoBill), "TryFindBestIngredientsHelper")]
        public static void TryFindBestIngredientsHelper_Prefix(Predicate<Thing> thingValidator, Pawn pawn)
        {
            _thingValidator = thingValidator;
            _pawn = pawn;
            _linkedDsu.Clear();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(WorkGiver_DoBill), "TryFindBestIngredientsInSet_NoMixHelper")]
        public static void TryFindBestIngredientsInSet_NoMixHelper_Postfix(ref bool __result)
        {
            Log.Warning($"--- TryFindBestIngredientsInSet_NoMixHelper_Postfix = {__result}");
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
        var sb = new StringBuilder();
        sb.AppendLine("-------------");
        var regionsProcessedFound = false;
        var portSearchAdded = false;

        foreach (var instruction in instructions)
        {
            sb.AppendLine($"{instruction.opcode} / {instruction.operand}");

            if (!regionsProcessedFound && instruction.opcode == OpCodes.Stfld && instruction.operand.ToString() == "System.Int32 regionsProcessed")
            {
                sb.AppendLine("^^^ regionsProcessedFound");
                regionsProcessedFound = true;
                yield return instruction;
                continue;
            }

            if (regionsProcessedFound && !portSearchAdded)
            {
                sb.AppendLine("^^^ portSearchAdded");
                yield return new CodeInstruction(OpCodes.Ldarg_1); // Region r
                // yield return new CodeInstruction(OpCodes.Ldsfld, _regionsProcessed);
                // yield return new CodeInstruction(OpCodes.Ldsfld, _adjacentRegionsAvailable);
                yield return new CodeInstruction(OpCodes.Ldc_I4, 0);
                yield return new CodeInstruction(OpCodes.Ldc_I4, 1);
                yield return new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(WorkGiver_DoBill), "relevantThings"));
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_WorkGiver_DoBill_DisplayClass_24_0_HiddenMethod), nameof(ProcessRegion)));

                portSearchAdded = true;
                yield return instruction;
                continue;
            }

            yield return instruction;
        }

        Log.Warning(sb.ToString());
    }

    public static void ProcessRegion(Region region, Int32 regionsProcessed, Int32 adjacentRegionsAvailable, List<Thing> relevantThings)
    {
        foreach (var thing in region.ListerThings.AllThings)
        {
            if (thing is not Building_AdvancedStorageUnitIOPort port) continue;
            Log.Warning($"--- DSU port found: {port}");
            if (port.PowerTrader.PowerOn && port.BoundStorageUnit is not null && port.BoundStorageUnit.CanWork)
            {
                Log.Warning("--- DSU port added");
                _linkedDsu.Add(port.BoundStorageUnit);
            }
        }

        if (regionsProcessed > adjacentRegionsAvailable)
        {
            Log.Warning($"--- DSU AddRelevantThings, _thingValidator = {_thingValidator}, _pawn = {_pawn}");
            if (_thingValidator is null || _pawn is null) return;

            Log.Warning($"--- DSU _linkedDsu = {string.Join(", ", _linkedDsu)}, relevantThings = {string.Join(", ", relevantThings)}");
            foreach (var dsu in _linkedDsu)
            {
                foreach (var item in dsu.StoredItems)
                {
                    if (!item.Spawned) continue;
                    if (item.IsForbidden(_pawn)) continue;
                    if (!_pawn.CanReserve(item)) continue;
                    if (!_thingValidator(item)) continue;
                    if (relevantThings.Contains(item)) continue;
                    relevantThings.Add(item);
                    Log.Warning($"--- DSU add dsu holded item: {item.LabelCap}");
                }
            }

            _thingValidator = null;
            _pawn = null;
        }
    }
}