using System.Diagnostics.CodeAnalysis;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

[SuppressMessage("ReSharper", "UnusedType.Global")] // def-injected
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
[SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Global")]
[SuppressMessage("ReSharper", "ConvertToConstant.Global")] // def-injected
public class DsuHeaterCompProperties : CompProperties
{
    public int HeatPerSecond = 10;
    public int MaxHeat = 1000;
    public int DamageAtHeat = 200;
    public float DamageMultiplier = 0.1f;

    public DsuHeaterCompProperties()
    {
        compClass = typeof(DsuHeaterComp);
    }
}