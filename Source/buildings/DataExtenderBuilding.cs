using System.Linq;
using DigitalStorageUnit.util;
using RimWorld;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

/// <summary>
/// TODO!
/// Снизить лимит хранения у ДСУ
/// Увеличивает лимит хранения на +100 стаков
/// Немного снижает нагрев ДСУ
/// </summary>
public class DataExtenderBuilding : Building
{
    private bool _firstTickProcessed;
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

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        _firstTickProcessed = false;
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        base.DeSpawn(mode);
        _boundStorageUnit = null;
        _boundStorageUnit?.Extenders.Remove(this);
        _firstTickProcessed = false;
        _compPowerTrader = null;
    }

    public override void Tick()
    {
        base.Tick();
        if (_firstTickProcessed && !this.IsHashIntervalTick(60)) return;

        var dsuList = this.GetRoom().ContainedAndAdjacentThings.Where(t => t is DigitalStorageUnitBuilding).Cast<DigitalStorageUnitBuilding>().ToList();
        var newDsu = dsuList.Count == 1 ? dsuList.FirstOrDefault() : null;
        if (newDsu != _boundStorageUnit)
        {
            _boundStorageUnit?.Extenders.Remove(this);
            _boundStorageUnit = newDsu;
            _boundStorageUnit?.Extenders.Add(this);
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