using System.Collections.Generic;
using DigitalStorageUnit.ui;
using UnityEngine;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

public class CompOutputAdjustable : ThingComp
{
    private int _index;
    private List<IntVec3> _possibleOutputs = new();

    public IntVec3 CurrentCell => _possibleOutputs[_index %= _possibleOutputs.Count];

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        _possibleOutputs = new List<IntVec3>(GenAdj.CellsAdjacent8Way(parent));
    }

    public override void PostDrawExtraSelectionOverlays()
    {
        GenDraw.DrawFieldEdges(new List<IntVec3> { CurrentCell }, Color.yellow);
    }

    private void RotateIndex(int step)
    {
        _index += step;
        if (_index < 0) _index = _possibleOutputs.Count - 1;
        if (_index >= _possibleOutputs.Count) _index = 0;
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        yield return new ActionRightLeftCommand
        {
            defaultLabel = "DSU.AdjustOutputDirection".Translate(),
            ActionL = () => RotateIndex(1),
            ActionR = () => RotateIndex(-1),
            icon = TexUI.RotRightTex,
            defaultIconColor = Color.green
        };
    }

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref _index, "outputSlotIndex");
    }
}