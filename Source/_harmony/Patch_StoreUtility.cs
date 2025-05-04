using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.util;
using HarmonyLib;
using RimWorld;
using Verse;

namespace DigitalStorageUnit._harmony;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[HarmonyPatch(typeof(StoreUtility))]
public static class Patch_StoreUtility
{
    /// <summary>
    ///     todo see Patch_StorageSettings, Patch_Building_Storage_Accepts
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch("TryFindBestBetterStoreCellForWorker")]
    public static bool Prefix(
        Thing t, Pawn carrier, Map map, Faction faction, ISlotGroup slotGroup,
        bool needAccurateResult, ref IntVec3 closestSlot, ref float closestDistSquared,
        ref StoragePriority foundPriority
    )
    {
        if (slotGroup is not SlotGroup sg) return true;
        if (sg.parent is not Building_Storage storage) return true;
        if (storage is not IForbidPawnInputItem forb) return true;
        return !forb.ForbidPawnInput;
    }
}