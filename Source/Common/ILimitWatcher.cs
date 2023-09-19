using Verse;

namespace DigitalStorageUnit.Common;

interface ILimitWatcher
{
    public bool ItemIsLimit(ThingDef thing, bool CntStacks, int limit);
}