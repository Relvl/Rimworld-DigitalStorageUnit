using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DigitalStorageUnit.ui;
using UnityEngine;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")] // def-injected
public class PortPositionComp : ThingComp
{
    /// <summary>
    /// -2   = all around
    /// -1   = current position
    /// 0..8 = round around
    /// </summary>
    private int _positionIdx = -1;

    private List<IntVec3> _roundAround;
    private List<IntVec3> _every;
    private List<IntVec3> _parent;
    private List<IntVec3> _directional;

    public override void PostSpawnSetup(bool respawningAfterLoad)
    {
        _roundAround = GenAdj.CellsAdjacent8Way(parent).ToList();
        _every = new List<IntVec3>(_roundAround) { parent.Position };
        _parent = new List<IntVec3> { parent.Position };
        RecacheDirectional();
    }

    public List<IntVec3> GetAvailablePositions()
    {
        return _positionIdx switch
        {
            -2 => _every,
            -1 => _parent,
            _ => _directional
        };
    }

    public override void PostDrawExtraSelectionOverlays()
    {
        GenDraw.DrawFieldEdges(GetAvailablePositions(), Color.yellow);
    }

    public override IEnumerable<Gizmo> CompGetGizmosExtra()
    {
        yield return new ActionRightLeftCommand
        {
            defaultLabel = "DSU.AdjustOutputDirection".Translate(),
            ActionL = RotateL,
            ActionR = RotateR,
            icon = TexUI.RotRightTex,
            defaultIconColor = Color.green
        };
    }

    private void RotateL()
    {
        _positionIdx--;
        if (_positionIdx < -2) _positionIdx = _roundAround.Count - 1;
        RecacheDirectional();
    }

    private void RotateR()
    {
        _positionIdx++;
        if (_positionIdx > _roundAround.Count - 1) _positionIdx = -2;
        RecacheDirectional();
    }

    private void RecacheDirectional() => _directional = _positionIdx >= 0 ? new List<IntVec3> { _roundAround[_positionIdx] } : new List<IntVec3>();

    public override void PostExposeData()
    {
        Scribe_Values.Look(ref _positionIdx, "positionIdx", -1, true);
    }
}