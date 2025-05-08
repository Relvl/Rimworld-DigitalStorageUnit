using System.Collections.Generic;
using System.Linq;
using DigitalStorageUnit.compat;
using DigitalStorageUnit.extensions;
using DigitalStorageUnit.util;
using RimWorld;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

[StaticConstructorOnStartup]
public abstract class ABasePortDsuBuilding : Building_Storage, IForbidPawnInputItem, ILwmDsLeaveMeAlonePlease
{
    private CompPowerTrader _powerTrader; // Todo! Consume some more each operation
    private DigitalStorageUnitBuilding _boundStorageUnit;

    protected IntVec3 WorkPosition => Position;

    public bool Powered => _powerTrader?.PowerOn ?? false;

    public virtual StorageIOMode IOMode => /*Actually none here*/StorageIOMode.Input;

    public DigitalStorageUnitBuilding BoundStorageUnit
    {
        get => _boundStorageUnit;
        set
        {
            _boundStorageUnit?.Ports.Remove(this);
            _boundStorageUnit = value;
            value?.Ports.Add(this);
        }
    }

    public virtual bool ForbidPawnInput => !Powered || BoundStorageUnit is null;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref _boundStorageUnit, "boundStorageUnit");
    }

    public override void PostMake()
    {
        base.PostMake();
        _powerTrader ??= GetComp<CompPowerTrader>();
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        _powerTrader ??= GetComp<CompPowerTrader>();

        if (BoundStorageUnit?.Map != map && (BoundStorageUnit?.Spawned ?? false))
        {
            BoundStorageUnit = null;
        }

        if (DigitalStorageUnit.Config.AutoBoundDsu && BoundStorageUnit is null && !respawningAfterLoad)
        {
            var dsuBuildings = Map.listerBuildings.AllBuildingsColonistOfClass<DigitalStorageUnitBuilding>().ToHashSet();
            if (dsuBuildings.Count == 1)
            {
                BoundStorageUnit = dsuBuildings.FirstOrDefault();
                Messages.Message("DSU.Message.AutoBoundTo".Translate(BoundStorageUnit!.LabelCap), this, MessageTypeDefOf.PositiveEvent);
            }
        }

        map.GetDsuComponent().RegisterBuilding(this);
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        BoundStorageUnit?.Ports.Remove(this);
        Map.GetDsuComponent().DeregisterBuilding(this);
        base.DeSpawn(mode);
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var g in base.GetGizmos()) yield return g;

        yield return new Command_Action
        {
            defaultLabel = "DSU.ItemSource".Translate() + ": " + (BoundStorageUnit?.LabelCap ?? "NoneBrackets".Translate()),
            action = () =>
            {
                var options = Map.listerBuildings.allBuildingsColonist //
                    .Where(b => b is DigitalStorageUnitBuilding)
                    .Select(b => new FloatMenuOption(b.LabelCap, () => SelectedPorts().ToList().ForEach(p => p.BoundStorageUnit = b as DigitalStorageUnitBuilding)))
                    .ToList();
                if (options.Count == 0)
                    options.Add(new FloatMenuOption("NoneBrackets".Translate(), null));
                Find.WindowStack.Add(new FloatMenu(options));
            },
            icon = TextureHolder.CargoPlatform
        };
    }

    private IEnumerable<ABasePortDsuBuilding> SelectedPorts()
    {
        var selectedPorts = Find.Selector.SelectedObjects.OfType<ABasePortDsuBuilding>().ToList();
        if (!selectedPorts.Contains(this)) selectedPorts.Add(this);
        return selectedPorts;
    }
}