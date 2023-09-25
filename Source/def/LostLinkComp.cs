using System;
using DigitalStorageUnit.util;
using RimWorld;
using UnityEngine;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

public class LostLinkComp : ThingComp
{
    private static readonly float BaseAlt = AltitudeLayer.MetaOverlays.AltitudeFor();

    public override void PostDraw()
    {
        base.PostDraw();
        if (parent is not Building_StorageUnitIOBase { NoConnectionAlert: true }) return;
        var drawPos = parent.TrueCenter();
        drawPos.y = BaseAlt + 3f / 64f * 5;
        var alpha = (float)(0.30000001192092896 + (Math.Sin((Time.realtimeSinceStartup + 397.0 * (parent.thingIDNumber % 571)) * 4.0) + 1.0) * 0.5 * 0.699999988079071);
        var material = FadedMaterialPool.FadedVersionOf(TextureHolder.MatLostLink, alpha);
        Graphics.DrawMesh(MeshPool.plane08, drawPos, Quaternion.identity, material, 0);
    }
}