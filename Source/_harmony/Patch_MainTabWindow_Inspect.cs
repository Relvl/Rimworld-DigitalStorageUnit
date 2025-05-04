using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DigitalStorageUnit._harmony;

[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[HarmonyPatch(typeof(MainTabWindow_Inspect))]
public static class Patch_MainTabWindow_Inspect
{
    private static ITab_Items cachedITabItems;

    /// <summary>
    ///     WTF?.. InspectTabBase has OnOpen method but hasn't OnClose...
    ///     I need to cleanup my cache!
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch("set_OpenTabType")]
    public static void OpenTabType_Setter(Type value)
    {
        // todo! change to reflection - if there are OnClose method - store it and call when closed
        if (value == typeof(ITab_Items)) cachedITabItems = Find.Selector.SingleSelectedThing.GetInspectTabs().OfType<ITab_Items>().FirstOrDefault();

        if (cachedITabItems is not null && value is null)
        {
            cachedITabItems.OnClose();
            cachedITabItems = null;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(MainTabWindow_Inspect.CloseOpenTab))]
    public static void CloseOpenTab()
    {
        cachedITabItems?.OnClose();
        cachedITabItems = null;
    }
}