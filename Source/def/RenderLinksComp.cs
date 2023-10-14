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
            GenDraw.DrawCircleOutline(iobase.TrueCenter(), TextureHolder.CircleRadius, SimpleColor.Yellow);
            GenDraw.DrawCircleOutline(iobase.BoundStorageUnit.TrueCenter(), TextureHolder.CircleRadius, SimpleColor.Yellow);
            GenDraw.DrawLineBetween(iobase.TrueCenter(), iobase.BoundStorageUnit.TrueCenter(), SimpleColor.Yellow, TextureHolder.LineWidth);
        }

        if (parent is DigitalStorageUnitBuilding dsu)
        {
            var drawDsu = false;

            foreach (var port in dsu.Ports)
            {
                drawDsu = true;
                GenDraw.DrawCircleOutline(port.TrueCenter(), TextureHolder.CircleRadius, SimpleColor.Yellow);
                GenDraw.DrawLineBetween(dsu.TrueCenter(), port.TrueCenter(), SimpleColor.Yellow, TextureHolder.LineWidth);
            }

            foreach (var extender in dsu.Extenders)
            {
                drawDsu = true;
                GenDraw.DrawCircleOutline(extender.TrueCenter(), TextureHolder.CircleRadius, SimpleColor.Cyan);
                GenDraw.DrawLineBetween(dsu.TrueCenter(), extender.TrueCenter(), SimpleColor.Cyan, TextureHolder.LineWidth);
            }

            if (drawDsu)
            {
                GenDraw.DrawCircleOutline(dsu.TrueCenter(), TextureHolder.CircleRadius, SimpleColor.Yellow);
            }
        }
    }
}