using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DigitalStorageUnit.extensions;
using DigitalStorageUnit.util;
using RimWorld;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

/// <summary>
///     TODO!
///     Снизить лимит хранения у ДСУ
///     Увеличивает лимит хранения на +100 стаков
///     Немного снижает нагрев ДСУ
/// </summary>
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")] // def
public class DataExtenderBuilding : Building
{
    private DigitalStorageUnitBuilding _boundStorageUnit;
    private CompPowerTrader _compPowerTrader;

    public bool Powered
    {
        get
        {
            _compPowerTrader ??= GetComp<CompPowerTrader>();
            return _compPowerTrader.PowerOn;
        }
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        base.DeSpawn(mode);
        _boundStorageUnit = null;
        _boundStorageUnit?.Extenders.Remove(this);
        _compPowerTrader = null;
    }

    protected override void Tick()
    {
        base.Tick();
        if (!this.IsHashIntervalTick(60)) return;

        var dsuListInRoom = Map.GetDsuComponent().DsuSet.Where(d => d.GetRoom() == this.GetRoom()).ToList();
        var newDsu = dsuListInRoom.Count == 1 ? dsuListInRoom.FirstOrDefault() : null;
        if (newDsu != _boundStorageUnit)
        {
            _boundStorageUnit?.Extenders.Remove(this);
            _boundStorageUnit = newDsu;
            _boundStorageUnit?.Extenders.Add(this);
            // yes, it also removes bound DSU if there is more than one DSU in a room.
        }
    }

    public override void DrawExtraSelectionOverlays()
    {
        base.DrawExtraSelectionOverlays();
        if (_boundStorageUnit is null) return;
        GenDraw.DrawCircleOutline(this.TrueCenter(), 0.4f, SimpleColor.Cyan);
        GenDraw.DrawLineBetween(this.TrueCenter(), _boundStorageUnit.TrueCenter(), SimpleColor.Cyan, TextureHolder.LineWidth);
    }
}