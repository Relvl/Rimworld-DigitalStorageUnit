using System.Diagnostics.CodeAnalysis;
using RimWorld;
using Verse;

namespace DigitalStorageUnit.def;

[SuppressMessage("ReSharper", "UnassignedField.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[DefOf]
public class DsuDefOf
{
    public static ThingDef DSU_InputBus_Building;
    
    static DsuDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof (DsuDefOf));
}