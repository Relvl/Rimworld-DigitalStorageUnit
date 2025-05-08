using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DigitalStorageUnit.compat;
using Verse;

namespace DigitalStorageUnit;

/// <summary>
///     This class loads before mod instantiates and allow to deal with defs database.
/// </summary>
[SuppressMessage("ReSharper", "UnusedType.Global")]
[StaticConstructorOnStartup]
public class ModInit
{
    static ModInit()
    {
        var tabsToRemove = new[] { "ITab_Storage", "ITab_DeepStorage_Inventory" };
        foreach (var thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            if (thingDef.thingClass is null) continue;

            // Removes "Storage" and LWM's "Inventory" inspection tabs from all the ports, bcz we don't need to deal with em
            if (typeof(IRemoveStorageInspectionTab).IsAssignableFrom(thingDef.thingClass))
            {
                if (thingDef.inspectorTabs != null)
                    for (var i = thingDef.inspectorTabs.Count - 1; i >= 0; i--)
                        if (tabsToRemove.Contains(thingDef.inspectorTabs[i].Name))
                            thingDef.inspectorTabs.RemoveAt(i);

                if (thingDef.inspectorTabsResolved != null)
                    for (var i = thingDef.inspectorTabsResolved.Count - 1; i >= 0; i--)
                        if (tabsToRemove.Contains(thingDef.inspectorTabsResolved[i].GetType().Name))
                            thingDef.inspectorTabsResolved.RemoveAt(i);
            }
        }
    }
}