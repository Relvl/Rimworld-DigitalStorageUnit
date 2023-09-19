using RimWorld;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common;

static class CommonGUIFunctions
{
    public static Texture GetThingTextue(Rect rect, Thing thing, out Color color)
    {
        color = thing.DrawColor;
        thing = thing.GetInnerIfMinified();
        if (!thing.def.uiIconPath.NullOrEmpty())
        {
            return (Texture)(object)thing.def.uiIcon;
        }

        if (thing is Pawn || thing is Corpse)
        {
            var pawn = thing as Pawn;
            if (pawn == null)
            {
                pawn = ((Corpse)thing).InnerPawn;
            }

            if (!pawn.RaceProps.Humanlike)
            {
                if (!pawn.Drawer.renderer.graphics.AllResolved)
                {
                    pawn.Drawer.renderer.graphics.ResolveAllGraphics();
                }

                var obj = pawn.Drawer.renderer.graphics.nakedGraphic.MatAt(Rot4.East);
                color = obj.color;
                return obj.mainTexture;
            }

            rect = rect.ScaledBy(1.8f);
            rect.y += 3f;
            rect = rect.Rounded();
            //Unsure if Rot4.North is eqivalent to the past
            return (Texture)(object)PortraitsCache.Get(pawn, new Vector2(((Rect)(rect)).width, ((Rect)(rect)).height), Rot4.North);
        }

        return thing.Graphic.ExtractInnerGraphicFor(thing).MatAt(thing.def.defaultPlacingRot).mainTexture;
    }

    public static void ThingIcon(Rect rect, Thing thing, Texture resolvedIcon, Color color, float alpha = 1f)
    {
        thing = thing.GetInnerIfMinified();
        GUI.color = color;
        var resolvedIconAngle = 0f;
        if (!thing.def.uiIconPath.NullOrEmpty())
        {
            resolvedIconAngle = thing.def.uiIconAngle;
            rect.position = rect.position + new Vector2(thing.def.uiIconOffset.x * ((Rect)(rect)).size.x, thing.def.uiIconOffset.y * ((Rect)(rect)).size.y);
        }
        else if (thing is Pawn || thing is Corpse)
        {
            var pawn = thing as Pawn;
            if (pawn == null)
            {
                pawn = ((Corpse)thing).InnerPawn;
            }

            if (pawn.RaceProps.Humanlike)
            {
                rect = rect.ScaledBy(1.8f);
                rect.y += 3f;
                rect = rect.Rounded();
            }
        }

        if (alpha != 1f)
        {
            var color2 = GUI.color;
            color2.a *= alpha;
            GUI.color = color2;
        }

        ThingIconWorker(rect, thing.def, resolvedIcon, resolvedIconAngle);
        GUI.color = Color.white;
    }

    private static void ThingIconWorker(Rect rect, ThingDef thingDef, Texture resolvedIcon, float resolvedIconAngle, float scale = 1f)
    {
        var texProportions = new Vector2(resolvedIcon.width, resolvedIcon.height);
        var texCoords = new Rect(0f, 0f, 1f, 1f);
        if (thingDef.graphicData != null)
        {
            texProportions = thingDef.graphicData.drawSize.RotatedBy(thingDef.defaultPlacingRot);
            if (thingDef.uiIconPath.NullOrEmpty() && thingDef.graphicData.linkFlags != 0)
            {
                texCoords = new Rect(0f, 0.5f, 0.25f, 0.25f);
            }
        }

        Widgets.DrawTextureFitted(rect, resolvedIcon, GenUI.IconDrawScale(thingDef) * scale, texProportions, texCoords, resolvedIconAngle);
    }
}