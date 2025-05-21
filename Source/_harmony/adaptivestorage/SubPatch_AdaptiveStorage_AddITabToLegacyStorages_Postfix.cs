using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.compat;
using Verse;

namespace DigitalStorageUnit._harmony;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static class SubPatch_AdaptiveStorage_AddITabToLegacyStorages_Postfix
{
    /// <summary>
    ///     Removes the Adaptive Storage Framework patch for our buildings, that adds "Content" inspection tab for Building_Storage things.
    /// </summary>
    public static bool Prefix(object[] __args)
    {
        if (__args[1] is Thing and (IRemoveStorageInspectionTab or DigitalStorageUnitBuilding)) return false;
        return true;
    }
}