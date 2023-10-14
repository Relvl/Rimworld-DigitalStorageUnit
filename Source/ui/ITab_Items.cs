using System;
using RimWorld;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DigitalStorageUnit.util;
using UnityEngine;
using Verse;
using Verse.Sound;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

// Somebody toucha my spaghet code
// Only do above if LWM is installed ofc - rider
[StaticConstructorOnStartup]
[SuppressMessage("ReSharper", "UnusedType.Global")]
public class ITab_Items : ITab
{
    private const float RowHight = 28f;
    private const int FramePadding = 10; // Same as ITab_Storage
    private const float ClearSearchButtonSize = 18;
    private const float ButtonSize = 24;

    /// <summary>
    /// MaxHitPoints is based on stats, so looks like we need cache.
    /// </summary>
    private static readonly Dictionary<Thing, int> ThingMaxHitPoints = new();

    /// <summary>
    /// Icons needs a cache too, too slow rendering.
    /// </summary>
    private static readonly Dictionary<Thing, ThingIconTextureData> ThingIconCache = new();

    private static DigitalStorageUnitBuilding _oldSelected;

    private Vector2 _scrollPos;
    private string _searchQuery = "";
    private List<Thing> _itemsToShow;
    private TaggedString _windowCaption = "";
    private float _windowCaptionSize = 30;
    private RowRectSet _rowRectSet;

    public ITab_Items()
    {
        size = new Vector2(480f, 480f); // todo! autocalculate width with inspector window
        labelKey = "DSU.IItemsTab";
    }

    private DigitalStorageUnitBuilding Selected => SelThing as DigitalStorageUnitBuilding;

    public override void OnOpen()
    {
        RecalculateList();
    }

    public void OnClose()
    {
        ThingMaxHitPoints.Clear();
        ThingIconCache.Clear();
    }

    private void RenderHeader(Rect frame, ref float curY)
    {
        Text.Anchor = TextAnchor.UpperLeft;

        // Items: N/M stacks (found: X)
        Text.Font = GameFont.Medium;
        var labelRect = new Rect(0, curY, frame.width, _windowCaptionSize);
        Widgets.Label(labelRect, _windowCaption);
        curY += labelRect.height + FramePadding / 2f;

        // Search string
        Text.Font = GameFont.Small;
        GUI.color = Color.white;
        var searchQueryRect = new Rect(0, curY, frame.width - FramePadding - ClearSearchButtonSize, 25);
        var newSearchQuery = Widgets.TextField(searchQueryRect, _searchQuery);
        if (newSearchQuery != _searchQuery)
        {
            _searchQuery = newSearchQuery;
            RecalculateList();
        }

        // Clear search button
        if (!string.IsNullOrEmpty(_searchQuery))
        {
            var clarSearchRect = new Rect(frame.width - ClearSearchButtonSize, curY + 3, ClearSearchButtonSize, ClearSearchButtonSize);
            if (Widgets.ButtonImage(clarSearchRect, TexButton.CloseXSmall, Color.red))
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
                _searchQuery = "";
                RecalculateList();
            }
        }

        curY += searchQueryRect.height + FramePadding;

        GUI.color = Color.white;
    }

    private bool ThingFIlterPredicate(Thing thing)
    {
        if (string.IsNullOrEmpty(_searchQuery)) return true;
        // Todo! defName
        return thing.GetInnerIfMinified().Label.ToLower().NormalizedFuzzyStrength(_searchQuery.ToLower()) < FuzzySearch.Strength.Strong;
    }

    // Todo! Call this if items changed, uniquename changed, 
    public void RecalculateList()
    {
        if (Selected is null) return;

        var stored = Selected.GetStoredThings().ToList();
        _itemsToShow = stored.Where(ThingFIlterPredicate)
            .OrderByDescending(ThingSortLabelPredicate)
            .ThenByDescending(ThingSortQualityPredicate)
            .ThenByDescending(ThingSortHitPointsPredicate)
            .ToList();

        _oldSelected = Selected;

        Text.Font = GameFont.Medium;
        _windowCaption = string.IsNullOrEmpty(_searchQuery)
            ? "DSU.IItemsTab.Caption".Translate(Selected.LabelCap, stored.Count, Selected.GetSlotLimit)
            : "DSU.IItemsTab.Caption.Filtered".Translate(Selected.LabelCap, stored.Count, Selected.GetSlotLimit, _itemsToShow.Count);
        _windowCaptionSize = Text.CalcHeight(_windowCaption, size.x - FramePadding * 2);
    }

    protected override void FillTab()
    {
        if (_itemsToShow == null || _oldSelected == null || _oldSelected != Selected)
        {
            RecalculateList();
        }

        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;

        var curY = 0f;
        var frame = new Rect(FramePadding, FramePadding, size.x - FramePadding * 2, size.y - FramePadding * 2);
        GUI.BeginGroup(frame);

        RenderHeader(frame, ref curY);

        var outRect = new Rect(0, curY, frame.width, frame.height - curY);
        var viewRect = new Rect(0f, 0f, outRect.width - /*scrollbar width*/16f, _itemsToShow!.Count * RowHight);

        // region Scrollview Start
        Widgets.BeginScrollView(outRect, ref _scrollPos, viewRect);

        if (_itemsToShow.Count < 1)
        {
            Widgets.Label(viewRect, "DSU.IItemsTab.Label.Empty".Translate());
        }

        _rowRectSet.Reset(0, viewRect.width);

        for (var idx = _itemsToShow.Count - 1; idx >= 0; idx--)
        {
            if (_rowRectSet.IsWithinViewbox(_scrollPos.y, outRect.height))
            {
                var thing = _itemsToShow[idx];
                DrawThingRow(thing);
            }

            _rowRectSet.Shift();
        }

        // endregion Scrollview End
        Widgets.EndScrollView();
        GUI.EndGroup();

        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private void DrawThingRow(Thing thing)
    {
        var labelMoCount = thing is Corpse ? thing.Label : GenLabel.ThingLabel(thing.GetInnerIfMinified(), thing.stackCount, false);
        var labelCap = labelMoCount.CapitalizeFirst(thing.def);

        // Info card button
        RenderRowInfoButton(thing);

        // Thing icon
        RenderRowIcon(thing);

        // Label + click
        if (Mouse.IsOver(_rowRectSet.LabelRect))
        {
            GUI.color = ITab_Pawn_Gear.HighlightColor;
            GUI.DrawTexture(_rowRectSet.LabelRect, TexUI.HighlightTex);
        }

        if (Widgets.ButtonInvisible(_rowRectSet.LabelRect))
        {
            Find.Selector.ClearSelection();
            Find.Selector.Select(thing);
        }

        Text.Anchor = TextAnchor.MiddleLeft;
        GUI.color = ITab_Pawn_Gear.ThingLabelColor; // todo! quality color
        Text.WordWrap = false;
        Widgets.Label(_rowRectSet.LabelRectInner, labelCap.Truncate(_rowRectSet.LabelRectInner.width));
        Text.WordWrap = true;
        if (!ThingMaxHitPoints.ContainsKey(thing)) ThingMaxHitPoints.Add(thing, thing.MaxHitPoints);
        TooltipHandler.TipRegion(_rowRectSet.LabelRect, thing.def.useHitPoints ? string.Concat(labelCap, "\n", thing.HitPoints, " / ", ThingMaxHitPoints[thing]) : labelCap);

        // Rotable thing
        var ticksUntilRot = Math.Min(int.MaxValue, thing.TryGetComp<CompRottable>()?.TicksUntilRotAtCurrentTemp ?? int.MaxValue);
        if (ticksUntilRot < 36000000)
        {
            GUI.color = Color.yellow;
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(_rowRectSet.RotableInfoRect, (ticksUntilRot / 60000f).ToString("0.#"));
            Text.Anchor = TextAnchor.MiddleLeft;
            GUI.color = Color.white;
            TooltipHandler.TipRegion(_rowRectSet.RotableInfoRect, "DaysUntilRotTip".Translate());
        }

        // Forbid thing checkbox
        var invCurForbidden = !thing.IsForbidden(Faction.OfPlayer);
        var invMemForbidden = invCurForbidden;
        TooltipHandler.TipRegion(_rowRectSet.ForbidButtonRect, invCurForbidden ? "CommandNotForbiddenDesc".Translate() : "CommandForbiddenDesc".Translate());
        Widgets.Checkbox(_rowRectSet.ForbidButtonRect.x, _rowRectSet.ForbidButtonRect.y, ref invCurForbidden);
        if (invCurForbidden != invMemForbidden)
            thing.SetForbidden(!invCurForbidden, false);

        // Drop thing button
        TooltipHandler.TipRegion(_rowRectSet.DropButtonRect, "DSU.Drop".Translate(labelMoCount));
        if (Widgets.ButtonImage(_rowRectSet.DropButtonRect, TexButton.Drop, Color.gray, Color.white, false))
            DropThing(thing);
    }

    private void RenderRowInfoButton(Thing thing)
    {
        var getDialog = () => new Dialog_InfoCard(thing);

        if (thing is IConstructible constructible)
            if (thing.def.entityDefToBuild is ThingDef entityDefToBuild)
                getDialog = () => new Dialog_InfoCard(entityDefToBuild, constructible.EntityToBuildStuff());
            else
                getDialog = () => new Dialog_InfoCard(thing.def.entityDefToBuild);

        TooltipHandler.TipRegionByKey(_rowRectSet.InfoRect, "DefInfoTip");
        UIHighlighter.HighlightOpportunity(_rowRectSet.InfoRect, "InfoCard");

        if (Widgets.ButtonImage(_rowRectSet.InfoRect, TexButton.Info, GUI.color))
        {
            SoundDefOf.Click.PlayOneShotOnCamera();
            Find.WindowStack.Add(getDialog());
        }
    }

    private void RenderRowIcon(Thing thing)
    {
        thing = thing.GetInnerIfMinified();

        if (thing.def.DrawMatSingle == null || thing.def.DrawMatSingle.mainTexture == null) return;

        if (!ThingIconCache.ContainsKey(thing)) ThingIconCache.Add(thing, new ThingIconTextureData(thing.GetThingTextue(out var color), color));
        var thingIconTextureData = ThingIconCache[thing];

        GUI.color = thingIconTextureData.Color;
        var resolvedIconAngle = 0f;

        var copyRect = _rowRectSet.IconRect;

        if (!thing.def.uiIconPath.NullOrEmpty())
        {
            resolvedIconAngle = thing.def.uiIconAngle;
            copyRect.position += new Vector2(thing.def.uiIconOffset.x * copyRect.size.x, thing.def.uiIconOffset.y * copyRect.size.y);
        }
        else if (thing is Pawn or Corpse)
        {
            var pawn = thing as Pawn ?? ((Corpse)thing).InnerPawn;
            if (pawn.RaceProps.Humanlike)
            {
                copyRect = copyRect.ScaledBy(1.8f);
                copyRect.y += 3f;
                copyRect = copyRect.Rounded();
            }
        }

        var texProportions = new Vector2(thingIconTextureData.Texture.width, thingIconTextureData.Texture.height);
        var texCoords = new Rect(0f, 0f, 1f, 1f);
        if (thing.def.graphicData != null)
        {
            texProportions = thing.def.graphicData.drawSize.RotatedBy(thing.def.defaultPlacingRot);
            if (thing.def.uiIconPath.NullOrEmpty() && thing.def.graphicData.linkFlags != 0)
            {
                texCoords = new Rect(0f, 0.5f, 0.25f, 0.25f);
            }
        }

        Widgets.DrawTextureFitted(copyRect, thingIconTextureData.Texture, GenUI.IconDrawScale(thing.def), texProportions, texCoords, resolvedIconAngle);

        GUI.color = Color.white;
    }

    private static string ThingSortLabelPredicate(Thing t) => t.Label;

    private static QualityCategory ThingSortQualityPredicate(Thing t)
    {
        t.TryGetQuality(out var quality);
        return quality;
    }

    private static int ThingSortHitPointsPredicate(Thing t) => t.HitPoints / t.MaxHitPoints;

    private void DropThing(Thing thing)
    {
        var cell = Selected.GetComp<DsuDropAdjustComp>()?.CurrentCell ?? Selected.Position + new IntVec3(0, 0, -2);
        var result = GenPlace.TryPlaceThing(
            thing.SplitOff(thing.stackCount),
            cell,
            Selected.Map,
            ThingPlaceMode.Near,
            null,
            vec => !vec.GetThingList(Selected.Map).Any(e => e is DigitalStorageUnitBuilding)
        );
        if (result) RecalculateList();
    }

    private struct RowRectSet
    {
        public Rect LabelRect;
        public Rect LabelRectInner;
        public Rect InfoRect;
        public Rect IconRect;
        public Rect RotableInfoRect;
        public Rect ForbidButtonRect;
        public Rect DropButtonRect;

        public void Shift(float height = RowHight)
        {
            LabelRect.y += height;
            LabelRectInner.y += height;
            InfoRect.y += height;
            IconRect.y += height;
            ForbidButtonRect.y += height;
            DropButtonRect.y += height;
            RotableInfoRect.y += height;
        }

        public void Reset(float initialY, float width)
        {
            LabelRect = new Rect(0, 0, width, RowHight);

            InfoRect = new Rect(LabelRect.x, initialY, ButtonSize, ButtonSize);
            LabelRect.xMin += ButtonSize + 2;

            IconRect = new Rect(LabelRect.x, initialY, ButtonSize, ButtonSize);

            DropButtonRect = new Rect(LabelRect.xMax - ButtonSize, initialY, ButtonSize, ButtonSize);
            LabelRect.width -= ButtonSize + 2;

            ForbidButtonRect = new Rect(LabelRect.xMax - ButtonSize, initialY, ButtonSize, ButtonSize);
            LabelRect.width -= ButtonSize + 2;

            RotableInfoRect = new Rect(LabelRect.xMax - 30 - FramePadding, initialY, 30, ButtonSize);

            LabelRectInner = LabelRect;
            LabelRectInner.xMin += ButtonSize + 4;
        }

        public bool IsWithinViewbox(float scrollPosition, float viewBoxHeight) => LabelRect.y + RowHight - scrollPosition >= 0 && LabelRect.y - scrollPosition < viewBoxHeight;
    }
}