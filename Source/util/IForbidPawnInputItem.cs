using RimWorld;

namespace DigitalStorageUnit.util;

public interface IForbidPawnInputItem : ISlotGroupParent
{
    bool ForbidPawnInput { get; }
}