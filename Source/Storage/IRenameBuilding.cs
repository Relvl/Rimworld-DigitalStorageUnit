using Verse;

namespace DigitalStorageUnit.Storage;

public interface IRenameBuilding
{
    public string UniqueName { set; get; }
    public Building Building { get; }
}