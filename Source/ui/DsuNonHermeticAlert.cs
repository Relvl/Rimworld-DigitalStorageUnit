using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DigitalStorageUnit.util;
using RimWorld;
using UnityEngine;
using Verse;

namespace DigitalStorageUnit.ui;

[SuppressMessage("ReSharper", "UnusedType.Global")] // vanilla-reflected
public class DsuNonHermeticAlert : Alert
{
    public override string GetLabel() => "DSU.Alert.DsuNonHermetic".Translate();
    public override TaggedString GetExplanation() => "DSU.Alert.DsuNonHermetic.Desc".Translate();
    protected override Color BGColor => TextureHolder.Red25;

    public override AlertReport GetReport()
    {
        if (!DigitalStorageUnit.Config.HeaterEnabled) return false;

        var unroofedDsus = Find.Maps.Where(m => m.IsPlayerHome)
            .SelectMany(m => m.listerBuildings.AllBuildingsColonistOfClass<DigitalStorageUnitBuilding>())
            .Where(
                t =>
                {
                    if (t is not { Powered: true }) return false;
                    var comp = t.TryGetComp<DsuHeaterComp>();
                    if (comp is null) return false;
                    return !comp.IsRoomHermetic();
                }
            )
            .Cast<Thing>()
            .ToList();

        return unroofedDsus.Any() ? AlertReport.CulpritsAre(unroofedDsus) : false;
    }
}