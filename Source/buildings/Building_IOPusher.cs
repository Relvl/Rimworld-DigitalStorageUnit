using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.util;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

[SuppressMessage("ReSharper", "UnusedType.Global")]
[SuppressMessage("ReSharper", "InconsistentNaming")]
public class Building_IOPusher : Building_StorageUnitIOBase
{
    public override IntVec3 WorkPosition => Position + Rotation.FacingCell;

    public override StorageIOMode IOMode
    {
        get => StorageIOMode.Output;
        set => _ = value;
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        ioMode = IOMode;
    }
}