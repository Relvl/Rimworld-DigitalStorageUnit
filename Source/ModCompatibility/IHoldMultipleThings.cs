using System.Diagnostics.CodeAnalysis;
using Verse;

// ReSharper disable CheckNamespace
namespace IHoldMultipleThings
{
    /// <summary>
    /// Thing from PUAH repository. Needed to works with PUAH, obviously.
    /// https://github.com/Mehni/PickUpAndHaul/blob/master/Source/IHoldMultipleThings/IHoldMultipleThings.cs
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public interface IHoldMultipleThings
    {
        bool CapacityAt(Thing thing, IntVec3 storeCell, Map map, out int capacity);

        bool StackableAt(Thing thing, IntVec3 storeCell, Map map);
    }
}