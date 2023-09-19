using RimWorld;

namespace DigitalStorageUnit.Common.HarmonyPatches;

public interface IForbidPawnInputItem : ISlotGroupParent
{
    bool ForbidPawnInput { get; }
}