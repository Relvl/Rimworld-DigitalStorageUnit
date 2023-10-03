using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.util;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class Building_IOPusher : Building_StorageUnitIOBase
{
    protected override IntVec3 WorkPosition => Position + Rotation.FacingCell;

    public override StorageIOMode IOMode
    {
        get => StorageIOMode.Output;
        protected set => _ = value;
    }
}