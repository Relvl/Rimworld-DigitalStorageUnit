using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DigitalStorageUnit.util;
using UnityEngine;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

[SuppressMessage("ReSharper", "UnusedType.Global")] // def-reflective
[SuppressMessage("ReSharper", "InconsistentNaming")]
[StaticConstructorOnStartup]
public class PlaceWorkerIOPusherHilight : PlaceWorker
{
    private static readonly Material arrow = FadedMaterialPool.FadedVersionOf(MaterialPool.MatFrom(TextureHolder.Arrow), .6f);

    public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
    {
        GenDraw.DrawFieldEdges(new List<IntVec3> { center + rot.FacingCell }, Color.yellow);

        var pos = center.ToVector3Shifted();
        pos.y = AltitudeLayer.LightingOverlay.AltitudeFor();
        Graphics.DrawMesh(MeshPool.plane10, pos, rot.AsQuat, arrow, 0);
    }
}