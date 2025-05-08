using System;
using UnityEngine;
using Verse;

namespace DigitalStorageUnit;

public class DigitalStorageUnitConfig : ModSettings
{
    // Please up this version only when breaking changes in the configs
    private const int Version = 1;

    public float DsuPathingMultiplier = 1;
    public bool CheapPathfinding = true;
    public bool HalfPathfinding = true;
    public float EnergyPerStack = 10;
    public bool HeaterEnabled;
    public bool WorkGiverDoBillUnnecessaryFix = true;
    public bool BillSearchRadiusFix = true;
    public bool AutoBoundDsu = true;
    public bool CleanCellItemList = true;

    public override void ExposeData()
    {
        var version = Version;
        Scribe_Values.Look(ref version, "Version", Version, true);
        if (version == Version || Scribe.mode == LoadSaveMode.LoadingVars)
        {
            Scribe_Values.Look(ref DsuPathingMultiplier, "DsuPathingMultiplier", 1, true);
            Scribe_Values.Look(ref CheapPathfinding, "CheapPathfinding", true, true);
            Scribe_Values.Look(ref HalfPathfinding, "HalfPathfinding", true, true);
            Scribe_Values.Look(ref EnergyPerStack, "EnergyPerStack", 10, true);
            Scribe_Values.Look(ref HeaterEnabled, "HeaterEnabled", false, true);
            Scribe_Values.Look(ref WorkGiverDoBillUnnecessaryFix, "WorkGiverDoBillUnnecessaryFix", true, true);
            Scribe_Values.Look(ref BillSearchRadiusFix, "BillSearchRadiusFix", true, true);
            Scribe_Values.Look(ref AutoBoundDsu, "AutoBoundDsu", true, true);
            Scribe_Values.Look(ref CleanCellItemList, "CleanCellItemList", true, true);
        }
        else
        {
            Log.Warning("DSU: version changed, config reset");
        }

        EnergyPerStack = Mathf.Clamp(EnergyPerStack, 0, 100000);
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
        else
        {
            list.CheckboxLabeled("DSU.Config.HalfPathfinding".Translate(), ref HalfPathfinding, tooltip: "DSU.Config.HalfPathfinding.Desc".Translate());
        }

        EnergyPerStack = (float)Math.Round(
            list.SliderLabeled("DSU.Config.EnergyPerStack".Translate(EnergyPerStack.ToString("0")), EnergyPerStack, 1, 50, tooltip: "DSU.Config.EnergyPerStack.Desc".Translate()),
            0
        );

        list.CheckboxLabeled("DSU.Config.HeaterEnabled".Translate(), ref HeaterEnabled, "DSU.Config.HeaterEnabled.Desc".Translate());

        list.CheckboxLabeled(
            "DSU.Config.WorkGiverDoBillUnnecessaryFix".Translate(),
            ref WorkGiverDoBillUnnecessaryFix,
            "DSU.Config.WorkGiverDoBillUnnecessaryFix.Desc".Translate()
        );

        list.CheckboxLabeled("DSU.Config.BillSearchRadiusFix".Translate(), ref BillSearchRadiusFix, "DSU.Config.BillSearchRadiusFix.Desc".Translate());

        list.CheckboxLabeled("DSU.Config.AutoBoundDsu".Translate(), ref AutoBoundDsu, "DSU.Config.AutoBoundDsu.Desc".Translate());
        
        list.CheckboxLabeled("DSU.Config.CleanCellItemList".Translate(), ref CleanCellItemList, "DSU.Config.CleanCellItemList.Desc".Translate());

        list.End();
    }
}