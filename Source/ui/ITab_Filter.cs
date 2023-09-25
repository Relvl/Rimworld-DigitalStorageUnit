using System.Diagnostics.CodeAnalysis;
using RimWorld;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

/// <summary>
/// Have an ITab_Storage that says "Filter" instead of "Storage"
/// Everything else is vanilla, so any changes anyone makes to ITab_Storage (such as RSA's search function!) *should* work just fine for us!
/// </summary>
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "ArrangeTypeModifiers")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
class ITab_Filter : ITab_Storage
{
    public ITab_Filter()
    {
        labelKey = "Filter";
    }
}