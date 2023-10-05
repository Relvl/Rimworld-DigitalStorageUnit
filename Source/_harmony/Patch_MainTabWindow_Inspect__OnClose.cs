using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DigitalStorageUnit._harmony;

/// <summary>
/// WTF?.. InspectTabBase has OnOpen method but hasn't OnClose...
/// I need to cleanup my cache!
/// </summary>
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[HarmonyPatch(typeof(MainTabWindow_Inspect))]
public static class Patch_MainTabWindow_Inspect__OnClose
{
    private static ITab_Items cachedItabItems;

    [HarmonyPrefix]
    [HarmonyPatch("set_OpenTabType")]
    public static void OpenTabType_Property_Prefix(Type value)
    {
        // todo! change to reflection - if there are OnClose method - store it and call when closed
        if (value == typeof(ITab_Items))
        {
            cachedItabItems = Find.Selector.SingleSelectedThing.GetInspectTabs().OfType<ITab_Items>().FirstOrDefault();
        }

        if (cachedItabItems is not null && value is null)
        {
            cachedItabItems.OnClose();
            cachedItabItems = null;
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(MainTabWindow_Inspect.CloseOpenTab))]
    public static void CloseOpenTab_Prefix()
    {
        cachedItabItems?.OnClose();
        cachedItabItems = null;
    }

    [HarmonyPrefix]
    [HarmonyPatch(nameof(MainTabWindow_Inspect.CloseOpenTab))]
    public static void Reset_Prefix()
    {
        cachedItabItems?.OnClose();
        cachedItabItems = null;
    }
}