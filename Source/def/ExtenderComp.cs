using System.Diagnostics.CodeAnalysis;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

/// <summary>
/// 
/// </summary>
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public class ExtenderComp : ThingComp
{
    public DigitalStorageUnitBuilding BoundDsu;
    
    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        base.PostSpawnSetup(respawningAfterLoad);
    }

    public override void CompTick()
    {
        base.CompTick();
    }

    public override void PostDrawExtraSelectionOverlays()
    {
        base.PostDrawExtraSelectionOverlays();
    }
}