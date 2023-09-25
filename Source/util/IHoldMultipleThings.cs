using Verse;

// ReSharper disable once CheckNamespace
namespace IHoldMultipleThings
{
    public interface IHoldMultipleThings
    {
        bool CapacityAt(Thing thing, IntVec3 storeCell, Map map, out int capacity);

        bool StackableAt(Thing thing, IntVec3 storeCell, Map map);
    }
}
