using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.def;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public class RenderLinkCompProperties : CompProperties
{
    public RenderLinkCompProperties()
    {
        compClass = typeof(RenderLinkComp);
    }
}