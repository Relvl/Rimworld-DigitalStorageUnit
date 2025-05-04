using RimWorld;
using UnityEngine;
using Verse;

namespace DigitalStorageUnit.util;

public static class ThingExtensions
{
    private static readonly Rect RenderRect = new Rect(0, 0, 24, 24).ScaledBy(1.8f).Rounded();

    public static Texture GetThingTexture(this Thing thing, out Color color)
    {
        color = thing.DrawColor;
        thing = thing.GetInnerIfMinified();
        if (!thing.def.uiIconPath.NullOrEmpty()) return thing.def.uiIcon;
        if (thing is not Pawn && thing is not Corpse) return thing.Graphic.ExtractInnerGraphicFor(thing).MatAt(thing.def.defaultPlacingRot).mainTexture;
        if (thing is not Pawn pawn) pawn = ((Corpse)thing).InnerPawn;
        if (pawn.RaceProps.Humanlike) return PortraitsCache.Get(pawn, new Vector2(RenderRect.width, RenderRect.height), Rot4.North);
        var graphic = pawn.Drawer.renderer.BodyGraphic;
        if (graphic == null) Log.Error($"PRF Can't get the Body Graphic for {pawn}");
        var material = graphic.MatAt(Rot4.East);
        color = material.color;
        return material.mainTexture;
    }
}