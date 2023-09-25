using System.Diagnostics.CodeAnalysis;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class LostLinkCompProperties : CompProperties
{
    public LostLinkCompProperties()
    {
        compClass = typeof(LostLinkComp);
    }
}