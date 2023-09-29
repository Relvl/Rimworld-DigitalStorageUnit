using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using DigitalStorageUnit.util;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DigitalStorageUnit._harmony;

/// <summary>
/// TODO! Description! Need more researches...
/// Something about find better Port for hungry pawn? 
/// </summary>
[HarmonyPatch(typeof(FoodUtility), "SpawnedFoodSearchInnerScan")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class Patch_FoodUtility_SpawnedFoodSearchInnerScan
{
    private static object _thingarg;
    private static bool _afterflaotMin;
    private static float mindist = float.MaxValue;
    private static Building_AdvancedStorageUnitIOPort closestPort;
    private static bool ioPortSelected;
    private static Thing ioPortSelectedFor;

    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        _thingarg = null;
        _afterflaotMin = false;

        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ldc_R4 && instruction.operand.ToString() == "-3.402823E+38")
            {
                yield return instruction;
                _afterflaotMin = true;
                continue;
            }

            if (_afterflaotMin && instruction.opcode == OpCodes.Stloc_S)
            {
                _afterflaotMin = false;
                yield return instruction;
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldarg_1);
                yield return new CodeInstruction(
                    OpCodes.Call,
                    AccessTools.Method(typeof(Patch_FoodUtility_SpawnedFoodSearchInnerScan), nameof(findClosestPort), new[] { typeof(Pawn), typeof(IntVec3) })
                );

                continue;
            }

            // TODO! Very ugly comparsion with op name...
            if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString() == "Verse.Thing (7)" && _thingarg == null)
            {
                _thingarg = instruction.operand;
            }

            // TODO! Very ugly comparsion with op name...
            if (instruction.opcode == OpCodes.Stloc_S && instruction.operand.ToString() == "System.Single (8)")
            {
                yield return instruction;

                yield return new CodeInstruction(OpCodes.Ldloca_S, instruction.operand);
                yield return new CodeInstruction(OpCodes.Ldloc_S, _thingarg);
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldarg_1);
                yield return new CodeInstruction(
                    OpCodes.Call,
                    AccessTools.Method(
                        typeof(Patch_FoodUtility_SpawnedFoodSearchInnerScan),
                        nameof(isIOPortBetter),
                        new[] { typeof(float).MakeByRefType(), typeof(Thing), typeof(Pawn), typeof(IntVec3) }
                    )
                );
                continue;
            }

            // TODO! Why not Postfix?
            if (instruction.opcode == OpCodes.Ret && _thingarg != null)
            {
                yield return new CodeInstruction(
                    OpCodes.Call,
                    AccessTools.Method(typeof(Patch_FoodUtility_SpawnedFoodSearchInnerScan), nameof(moveItemIfNeeded), new[] { typeof(Thing) })
                );
                yield return new CodeInstruction(OpCodes.Ldloc_3);
            }

            yield return instruction;
        }
    }

    public static void findClosestPort(Pawn pawn, IntVec3 root)
    {
        mindist = float.MaxValue;
        closestPort = null;

        if (pawn.Faction is not { IsPlayer: true }) return;

        //TODO: Not Optimal for the search. might need update
        var closest = AdvancedIOPatchHelper.GetClosestPort(pawn.Map, pawn.Position);
        mindist = closest.Key;
        closestPort = closest.Value;
    }

    public static void isIOPortBetter(ref float Distance, Thing thing, Pawn pawn, IntVec3 start)
    {
        ioPortSelected = false;

        // If the Port is Closer then it is a better choice
        // If the Port is the only Option it must be used
        if (mindist < Distance ||
            (pawn.Map.reachability.CanReach(start, thing, Verse.AI.PathEndMode.Touch, TraverseParms.For(pawn)) && Patch_Reachability_CanReach.CanReachThingViaAccessPonit(thing)))
        {
            //Check if the Port can be used
            //TODO: Check TODO in Line 93
            if (closestPort != null && AdvancedIOPatchHelper.CanMoveItem(closestPort, thing))
            {
                Distance = mindist;
                ioPortSelected = true;
                ioPortSelectedFor = thing;
            }
        }
    }

    public static void moveItemIfNeeded(Thing thing)
    {
        // When using replimat it might replace thing  
        if (thing != ioPortSelectedFor || !ioPortSelected || thing == null) return;

        ioPortSelected = false;
        closestPort.PlaceThingNow(thing);
    }
}