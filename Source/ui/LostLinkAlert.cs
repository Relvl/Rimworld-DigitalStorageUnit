using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DigitalStorageUnit.util;
using RimWorld;
using UnityEngine;
using Verse;

namespace DigitalStorageUnit.ui;

[SuppressMessage("ReSharper", "UnusedType.Global")] // vanilla-reflected
public class LostLinkAlert : Alert
{
    public override string GetLabel() => "DSU.Alert.LostLink.Label".Translate();
    public override TaggedString GetExplanation() => "DSU.Alert.LostLink.Desc".Translate();

    protected override Color BGColor => TextureHolder.Yellow25;

    public override AlertReport GetReport()
    {
        // todo! should divide ticks or cache all the ports
        var portsWithoutLink = Find.Maps.Where(m => m.IsPlayerHome)
            .SelectMany(m => m.listerBuildings.allBuildingsColonist)
            .Where(t => t is ABasePortDsuBuilding { BoundStorageUnit: null })
            .Cast<Thing>()
            .ToList();

        if (!portsWithoutLink.Any()) return false;

        return AlertReport.CulpritsAre(portsWithoutLink);
    }
}