using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.util;
using RimWorld;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class ITab_IOPortStorage : ITab_Storage
{
    public override bool IsVisible => SelThing is Building_StorageUnitIOBase { ioMode: StorageIOMode.Output };

    public ITab_IOPortStorage()
    {
        size = new Vector2(300f, 480f);
        labelKey = "TabStorage";
    }
}