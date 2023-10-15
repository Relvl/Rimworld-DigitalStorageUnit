using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DigitalStorageUnit.util;
using Verse;

namespace DigitalStorageUnit;

/// <summary>
/// I dunno why, but this class will load _some_ later than Mod instance...
/// </summary>
[SuppressMessage("ReSharper", "UnusedType.Global")]
[StaticConstructorOnStartup]
public class ModInit
{
    static ModInit()
    {
        if (!DigitalStorageUnit.IsDeepStorage) return;

        foreach (var thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
        {
            if (thingDef.thingClass is null) continue;
            if (!thingDef.thingClass.GetInterfaces().Contains(typeof(ILwmDsLeaveMeAlonePlease))) continue;

            for (var i = thingDef.inspectorTabs.Count - 1; i >= 0; i--)
            {
                if (thingDef.inspectorTabs[i].Name == "ITab_DeepStorage_Inventory")
                {
                    thingDef.inspectorTabs.RemoveAt(i);
                }
            }

            for (var i = thingDef.inspectorTabsResolved.Count - 1; i >= 0; i--)
            {
                if (thingDef.inspectorTabsResolved[i].GetType().Name == "ITab_DeepStorage_Inventory")
                {
                    thingDef.inspectorTabsResolved.RemoveAt(i);
                }
            }
        }

        Log.Warning("DSU: LWM.DeepStorage.ITab_DeepStorage_Inventory removed from the mod buildings");
    }
}