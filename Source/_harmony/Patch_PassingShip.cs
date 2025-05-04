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

namespace DigitalStorageUnit._harmony;

[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[HarmonyPatch]
public static class Patch_PassingShip
{
    private static Type hiddenClass;

    [HarmonyTargetMethod]
    public static MethodBase TargetMethod() //The target method is found using the custom logic defined here
    {
        hiddenClass = typeof(PassingShip).GetNestedTypes(AccessTools.all).FirstOrDefault(t => t.FullName!.Contains("c__DisplayClass23_0"));
        if (hiddenClass == null)
        {
            Log.Error("DSU Harmony Error - predicateClass == null for Patch_PassingShip_DSUisTradebeacon.TargetMethod()");
            return null;
        }

        var methodInfo = hiddenClass.GetMethods(AccessTools.all).FirstOrDefault(t => t.Name.Contains("b__1"));
        if (methodInfo == null)
        {
            Log.Error("DSU Harmony Error - methodInfo == null for Patch_PassingShip_DSUisTradebeacon.TargetMethod()");
        }

        return methodInfo;
    }

    /// <summary>
    /// This Patch Allows the player to start an orbital Trade without a Trade beacon but with a DSU.
    /// Without this patch a player would need a dummy beacon to use items in the orbital trade.
    /// todo! configs!
    /// </summary>
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var foundLocaterString = false;
        foreach (var instruction in instructions)
        {
            // Patch shall change:
            // if (!Building_OrbitalTradeBeacon.AllPowered(<>4__this.Map).Any())
            //
            // To:
            // if (!Building_OrbitalTradeBeacon.AllPowered(<>4__this.Map).Any() && !DigitalStorageUnitBuilding.AllPowered(<>4__this.Map).Any() )

            // Find the refrence Point
            if (instruction.opcode == OpCodes.Call &&
                instruction.operand as MethodInfo == AccessTools.Method(typeof(Building_OrbitalTradeBeacon), nameof(Building_OrbitalTradeBeacon.AllPowered)))
            {
                foundLocaterString = true;
            }

            // Find the Check
            if (instruction.opcode == OpCodes.Brtrue_S && foundLocaterString)
            {
                foundLocaterString = false;
                //Keep the Inctruction
                yield return instruction;
                //this.Map
                yield return new CodeInstruction(OpCodes.Ldarg_0);
                yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(hiddenClass, "<>4__this"));
                yield return new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(PassingShip), "Map")); // PassingShip::Map
                //Call --> DigitalStorageUnitBuilding.AnyPowerd with the above as an argument
                yield return new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Patch_PassingShip), nameof(AnyPowered), [typeof(Map)]));
                yield return new CodeInstruction(OpCodes.Brtrue_S, instruction.operand);
                
                Log.Message("DSU: Patch_PassingShip_c__DisplayClass24_0 OK");
                continue;
            }

            // Keep the other instructions
            yield return instruction;
        }
    }

    public static bool AnyPowered(Map map) => map.GetAllPoweredDSU(true).Any();
}