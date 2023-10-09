using System;
using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.util;
using RimWorld;
using UnityEngine;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

[SuppressMessage("ReSharper", "UnusedType.Global")] // def-injected
public class RenderLinksComp : ThingComp
{
    private const float LineWidth = 0.1f;
    private const float CircleRadius = 0.8f;
    private static readonly float BaseAlt = AltitudeLayer.MetaOverlays.AltitudeFor();

    public override void PostDraw()
    {
        base.PostDraw();
        if (parent is not ABasePortDsuBuilding { Powered: true, BoundStorageUnit: null }) return;
        var drawPos = parent.TrueCenter();
        drawPos.y = BaseAlt + 3f / 64f * 5;
        var alpha = (float)(0.30000001192092896 + (Math.Sin((Time.realtimeSinceStartup + 397.0 * (parent.thingIDNumber % 571)) * 4.0) + 1.0) * 0.5 * 0.699999988079071);
        var material = FadedMaterialPool.FadedVersionOf(TextureHolder.MatLostLink, alpha);
        Graphics.DrawMesh(MeshPool.plane08, drawPos, Quaternion.identity, material, 0);
    }

    public override void PostDrawExtraSelectionOverlays()
    {
        if (parent is ABasePortDsuBuilding { BoundStorageUnit: { } } iobase)
        {
            GenDraw.DrawCircleOutline(iobase.TrueCenter(), CircleRadius, SimpleColor.Yellow);
            GenDraw.DrawCircleOutline(iobase.BoundStorageUnit.TrueCenter(), CircleRadius, SimpleColor.Yellow);
            GenDraw.DrawLineBetween(iobase.TrueCenter(), iobase.BoundStorageUnit.TrueCenter(), SimpleColor.Yellow, LineWidth);
        }

        if (parent is DigitalStorageUnitBuilding dsu)
        {
            if (dsu.Ports.Any())
            {
                GenDraw.DrawCircleOutline(dsu.TrueCenter(), CircleRadius, SimpleColor.Yellow);
                foreach (var port in dsu.Ports)
                {
                    GenDraw.DrawCircleOutline(port.TrueCenter(), CircleRadius, SimpleColor.Yellow);
                    GenDraw.DrawLineBetween(dsu.TrueCenter(), port.TrueCenter(), SimpleColor.Yellow, LineWidth);
                }
            }
        }
    }
}