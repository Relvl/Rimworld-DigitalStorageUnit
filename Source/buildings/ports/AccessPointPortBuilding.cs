using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DigitalStorageUnit.compat;
using DigitalStorageUnit.util;
using RimWorld;
using UnityEngine;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")] // def-injected
public class AccessPointPortBuilding : ABasePortDsuBuilding, IRemoveStorageInspectionTab
{
    public override StorageIOMode IOMode => StorageIOMode.Output;

    public override Graphic Graphic => base.Graphic.GetColoredVersion(base.Graphic.Shader, def.GetModExtension<ModExtensionPortColor>().outColor, Color.white);

    /// <summary>
    /// Disallow pawns to store items to this port
    /// </summary>
    public override bool ForbidPawnInput => true;

    public void ProvideItem(Thing thing)
    {
        if (BoundStorageUnit is null) return;
        if (!Powered) return;
        thing.Position = WorkPosition;
        FleckMaker.ThrowLightningGlow(WorkPosition.ToVector3(), Map, 0.8f);
    }

    protected override void Tick()
    {
        if (!this.IsHashIntervalTick(10)) return;
        if (!Powered) return;
        if (BoundStorageUnit is null || !BoundStorageUnit.CanWork) return;

        // Suck back unreserved items back to DSU 
        foreach (var thing in Map.thingGrid.ThingsListAt(WorkPosition).ToList()) // Todo! FirstHaulable
        {
            if (!thing.def.EverStorable(false)) continue;
            if (!BoundStorageUnit.CanReceiveThing(thing)) continue;
            if (Map.reservationManager.AllReservedThings().Contains(thing)) continue;
            BoundStorageUnit.HandleNewItem(thing);
        }
    }
}