using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DigitalStorageUnit.util;
using RimWorld;
using UnityEngine;
using Verse;

namespace DigitalStorageUnit.ui;

[SuppressMessage("ReSharper", "UnusedType.Global")] // vanilla-reflected
public class DsuOverheatAlert : Alert
{
    public override string GetLabel() => "DSU.Alert.DsuOverheat".Translate();
    public override TaggedString GetExplanation() => "DSU.Alert.DsuOverheat.Desc".Translate();
    protected override Color BGColor => TextureHolder.Red25;

    public override AlertReport GetReport()
    {
        if (!DigitalStorageUnit.Config.HeaterEnabled) return false;

        var poweredDsuComps = Find.Maps.Where(m => m.IsPlayerHome)
            .SelectMany(m => m.listerBuildings.allBuildingsColonist)
            .Where(t => t is DigitalStorageUnitBuilding { Powered: true })
            .Select(d => d.TryGetComp<DsuHeaterComp>())
            .Where(c => c.IsOverheat())
            .Select(c => c.parent as Thing)
            .ToList();

        return poweredDsuComps.Any() ? AlertReport.CulpritsAre(poweredDsuComps) : false;
    }
}