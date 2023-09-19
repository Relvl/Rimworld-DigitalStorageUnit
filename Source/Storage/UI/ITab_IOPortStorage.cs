using RimWorld;
using UnityEngine;

namespace DigitalStorageUnit.Storage.UI;

public class ITab_IOPortStorage : ITab_Storage
{
    public Building_StorageUnitIOBase SelBuilding => (Building_StorageUnitIOBase)SelThing;
    public override bool IsVisible => SelBuilding != null && SelBuilding.mode == StorageIOMode.Output;

    public ITab_IOPortStorage()
    {
        size = new Vector2(300f, 480f);
        labelKey = "TabStorage";
    }
}