using System;
using System.Linq;
using RimWorld;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

public class DsuHeaterComp : ThingComp
{
    private DsuHeaterCompProperties Props => props as DsuHeaterCompProperties;
    private DigitalStorageUnitBuilding Dsu => parent as DigitalStorageUnitBuilding;
    private float _curTickTemperature;
    private bool _criticalDamageMessage = false;

    /// <summary>
    /// Each tick (1/60 sec)
    /// </summary>
    public override void CompTick()
    {
        if (!DigitalStorageUnit.Config.HeaterEnabled) return;
        if (Dsu is null) return;
        if (!Dsu.IsHashIntervalTick(60)) return;
        if (!Dsu.Powered) return;

        _curTickTemperature = GenTemperature.GetTemperatureForCell(Dsu.PositionHeld, Dsu.Map);
        if (_curTickTemperature >= Props.MaxHeat) return;

        var heat = Props.HeatPerSecond + _curTickTemperature;
        GenTemperature.PushHeat(Dsu.PositionHeld, Dsu.Map, heat);

        var isSecTick = Dsu.IsHashIntervalTick(60);
        if (_curTickTemperature > Props.DamageAtHeat)
        {
            if (isSecTick)
            {
                var damage = Props.DamageMultiplier * (_curTickTemperature - Props.DamageAtHeat);
                var damageInfo = new DamageInfo(DamageDefOf.Burn, damage, instigator: Dsu);
                Dsu.TakeDamage(damageInfo);
                Dsu.StoredItems.ToList().ForEach(i => i.TakeDamage(damageInfo));
                // Todo! Damage to all the pawns in the room
                // Todo! Damage to all the buildings in the room
            }
        }
        else if (_curTickTemperature > Props.DamageAtHeat / 2f)
        {
            if (isSecTick)
            {
                FleckMaker.ThrowSmoke(Dsu.Position.ToVector3Shifted(), Dsu.Map, (float)Math.Round(parent.def.size.x * parent.def.size.z / 4d, 1));
            }
        }
        else if (_curTickTemperature > Props.DamageAtHeat / 1.15f && !_criticalDamageMessage)
        {
            Find.LetterStack.ReceiveLetter("DSU.Alert.DsuOverheat".Translate(), "DSU.Alert.DsuOverheat.Desc".Translate(), LetterDefOf.ThreatBig, parent);
            _criticalDamageMessage = true;
        }
        else
        {
            _criticalDamageMessage = false;
        }
    }

    public bool IsRoomHermetic()
    {
        var room = Dsu.PositionHeld.GetRoom(Dsu.Map);
        if (room is null) return false;
        return room.OpenRoofCount <= 0 && !room.TouchesMapEdge;
    }

    public bool IsOverheat() => _curTickTemperature > Props.DamageAtHeat / 1.5f;
}