using RimWorld;
using UnityEngine;
using Verse;

namespace DigitalStorageUnit.util;

public static class ThingExtensions
{
    private static readonly Vector2 RenderRect = new Rect(0, 0, 24, 24).ScaledBy(1.8f).Rounded().size;

    public static Texture GetThingTextue(this Thing thing, out Color color)
    {
        color = thing.DrawColor;
        thing = thing.GetInnerIfMinified();
        if (!thing.def.uiIconPath.NullOrEmpty()) return thing.def.uiIcon;

        if (thing is not (Pawn or Corpse)) return thing.Graphic.ExtractInnerGraphicFor(thing).MatAt(thing.def.defaultPlacingRot).mainTexture;
        if (thing is not Pawn pawn) pawn = ((Corpse)thing).InnerPawn;

        if (pawn.RaceProps.Humanlike) return PortraitsCache.Get(pawn, RenderRect, Rot4.North);

        if (!pawn.Drawer.renderer.graphics.AllResolved) pawn.Drawer.renderer.graphics.ResolveAllGraphics();
        var material = pawn.Drawer.renderer.graphics.nakedGraphic.MatAt(Rot4.East);
        color = material.color;
        return material.mainTexture;
    }
}