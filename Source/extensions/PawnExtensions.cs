using Verse;

namespace DigitalStorageUnit.extensions;

public static class PawnExtensions
{
    public static bool IsDSUOnPoint(this Pawn pawn, IntVec3 cell)
    {
        return pawn?.Map.IsDSUOnPoint(cell) ?? false;
    }
}