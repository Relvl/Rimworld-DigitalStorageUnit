using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Verse;

namespace DigitalStorageUnit._harmony;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[HarmonyPatch(typeof(MouseoverReadout))]
public static class Patch_MouseoverReadout
{
    /// <summary>
    ///     Removes too large list of items under the DSU on left bottom listing
    /// </summary>
    [HarmonyTranspiler]
    [HarmonyPatch(nameof(MouseoverReadout.MouseoverReadoutOnGUI))]
    public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var cm = new CodeMatcher(instructions);
        /*
         List<Thing> thingList = intVec3.GetThingList(Find.CurrentMap);
         IL:
         IL_038a: ldloc.0      // intVec3
         IL_038b: call         class Verse.Map Verse.Find::get_CurrentMap()
         IL_0390: call         class [mscorlib]System.Collections.Generic.List`1<class Verse.Thing> Verse.GridsUtility::GetThingList(valuetype Verse.IntVec3, class Verse.Map)
         IL_0395: stloc.s      thingList
            // <-- inject here - stloc.s(4) = Instrumented_FixList(ldloc.s(4)))
                // why 4? okay, we need to understand which index it uses first.
                // see to the start of the IL method, there is something like that:
                //
                // .locals init (
                // [0] valuetype Verse.IntVec3 intVec3,
                // ....
                // [4] class [mscorlib]System.Collections.Generic.List`1<class Verse.Thing> thingList,
                //
                // we need that 4th index here - thingList.

         */
        cm.MatchEndForward(
            CodeMatch.IsLdloc(),
            CodeMatch.Calls(AccessTools.PropertyGetter(typeof(Find), nameof(Find.CurrentMap))),
            CodeMatch.Calls(AccessTools.Method(typeof(GridsUtility), nameof(GridsUtility.GetThingList))),
            CodeMatch.IsStloc()
        );

        if (!cm.IsValid)
        {
            Log.Error("DSU: Patch_MouseoverReadout: Failed to find matching load local cell");
            return cm.InstructionEnumeration();
        }

        // We need to insert AFTER stloc.s, but MatchEndForward stops before last match.
        cm.Advance(1);
        // Calls Instrumented_FixList and replaces value of 4th local var with it's result.
        cm.Insert(
            CodeInstruction.LoadLocal(4),
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Patch_MouseoverReadout), nameof(Instrumented_FixList))),
            CodeInstruction.StoreLocal(4)
        );

        return cm.InstructionEnumeration();
    }

    /// <summary>
    ///     If DSU building is present in the list - return new list with only this building.
    /// </summary>
    public static List<Thing> Instrumented_FixList(List<Thing> list)
    {
        if (DigitalStorageUnit.Config.CleanCellItemList && list.Any(t => t is DigitalStorageUnitBuilding))
            return list.Where(t => t.GetType() == typeof(DigitalStorageUnitBuilding)).ToList();
        return list;
    }
}