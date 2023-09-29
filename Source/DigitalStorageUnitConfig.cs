using System;
using System.Globalization;
using UnityEngine;
using Verse;

namespace DigitalStorageUnit;

public class DigitalStorageUnitConfig : ModSettings
{
    // Please up this version only when breaking changes in the configs
    private const int Version = 1;
    public static DigitalStorageUnit ModInstance;

    public float DsuPathingMultiplier = 1;
    public bool CheapPathfinding = true;

    public override void ExposeData()
    {
        var version = Version;
        Scribe_Values.Look(ref version, "Version", Version, true);
        if (version == Version || Scribe.mode == LoadSaveMode.LoadingVars)
        {
            Scribe_Values.Look(ref DsuPathingMultiplier, "DsuPathingMultiplier", 1, true);
        }
        else
        {
            Log.Warning("DSU: version changed, config reset");
        }
    }

    public void DoSettingsWindowContents(Rect inRect)
    {
        var list = new Listing_Standard(GameFont.Small);
        list.Begin(inRect);

        list.CheckboxLabeled(
            "DSU.Config.UseCheapPathfinding".Translate(),
            ref CheapPathfinding,
            "DSU.Config.UseCheapPathfinding.Desc".Translate() + "DSU.Config.UseCheapPathfinding.Desc.Caution".Translate().Colorize(Color.red)
        );

        if (CheapPathfinding)
        {
            DsuPathingMultiplier = (float)Math.Round(
                list.SliderLabeled(
                    "DSU.Config.DsuPathingMultiplier".Translate(DsuPathingMultiplier.ToString("0.0")),
                    DsuPathingMultiplier,
                    0.8f,
                    5.0f,
                    tooltip: "DSU.Config.DsuPathingMultiplier.Desc".Translate()
                ),
                1
            );
        }

        list.End();
    }
}