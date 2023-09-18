using ProjectRimFactory.SAL3;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common
{
    public class CompPowerWorkSetting : ThingComp, IPowerSupplyMachine
    {
        public CompProperties_PowerWorkSetting Props => (CompProperties_PowerWorkSetting)props;

        public int MaxPowerForSpeed => (int)(Props.floatrange_SpeedFactor.Span * Props.powerPerStepSpeed);

        public int MaxPowerForRange => (int)(Props.floatrange_Range.Span * Props.powerPerStepRange);

        public IRangeCells rangeCells = null;

        public float SupplyPowerForSpeed
        {
            get => powerForSpeed;
            set
            {
                powerForSpeed = value;
                AdjustPower();
                RefreshPowerStatus();
            }
        }

        public float SupplyPowerForRange
        {
            get => powerForRange;
            set
            {
                powerForRange = value;
                AdjustPower();
                RefreshPowerStatus();
            }
        }

        public virtual bool Glowable => false;

        public virtual bool Glow
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }

        public virtual bool SpeedSetting
        {
            get
            {
                if (SpeedSettingHide) return false;
                return Props.floatrange_SpeedFactor.Span > 0;
            }
        }

        public bool SpeedSettingHide = false;

        public bool RangeSettingHide = false;

        public bool RangeSetting
        {
            get
            {
                if (RangeSettingHide) return false;
                return Props.floatrange_Range.Span > 0;
            }
        }

        private float powerForSpeed = 0;

        public Rot4 RangeTypeRot = Rot4.North;

        private float powerForRange = 0;

        private enum rangeTypeClassEnum
        {
            CircleRange,
            FacingRectRange,
            RectRange
        }

        private int rangeTypeSelection
        {
            get
            {
                if (rangeCells == null) rangeCells = (IRangeCells)Activator.CreateInstance(Props.rangeType);
                if (rangeCells.ToText() == new CircleRange().ToText()) return (int)rangeTypeClassEnum.CircleRange;
                if (rangeCells.ToText() == new FacingRectRange().ToText()) return (int)rangeTypeClassEnum.FacingRectRange;
                if (rangeCells.ToText() == new RectRange().ToText()) return (int)rangeTypeClassEnum.RectRange;
                return (int)rangeTypeClassEnum.RectRange;
            }

            set
            {
                if (value == (int)rangeTypeClassEnum.CircleRange) rangeCells = new CircleRange();
                if (value == (int)rangeTypeClassEnum.FacingRectRange) rangeCells = new FacingRectRange();
                if (value == (int)rangeTypeClassEnum.RectRange) rangeCells = new RectRange();
            }
        }

        public float BasePowerConsumption => (float)ReflectionUtility.CompProperties_Power_basePowerConsumption.GetValue(powerComp.Props);

        public int CurrentPowerConsumption => (int)powerComp.PowerOutput;

        public Dictionary<string, int> AdditionalPowerConsumption => (parent as IAdditionalPowerConsumption)?.AdditionalPowerConsumption ?? null;

        private int AdditionalPowerDrain
        {
            get
            {
                if (AdditionalPowerConsumption != null && AdditionalPowerConsumption.Count > 0)
                {
                    return AdditionalPowerConsumption.Values.ToList().Sum();
                }

                return 0;
            }
        }

        public float PowerPerStepSpeed => Props.powerPerStepSpeed / Props.powerStepFactor;

        public float PowerPerStepRange => Props.powerPerStepRange;

        public FloatRange FloatRange_Range => Props.floatrange_Range;

        public float CurrentRange => GetRange();

        public FloatRange FloatRange_SpeedFactor => Props.floatrange_SpeedFactor;

        public float CurrentSpeedFactor => GetSpeedFactor();

        //Used for Saving the rangeCells . This is done as directly saving rangeCells leads to unknown Type Errors on Load
        private int rangeTypeSeletion = -1;

        public override void PostExposeData()
        {
            base.PostExposeData();

            //Load the Current rangeCells Value
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                rangeTypeSeletion = rangeTypeSelection;
            }

            Scribe_Values.Look<bool>(ref RangeSettingHide, "RangeSettingHide", false);
            Scribe_Values.Look<float>(ref powerForSpeed, "powerForSpeed");
            Scribe_Values.Look<float>(ref powerForRange, "powerForRange");
            Scribe_Values.Look(ref rangeTypeSeletion, "rangeType", -1);
            Scribe_Values.Look(ref RangeTypeRot, "RangeTypeRot", Rot4.North);

            //Set the Loaded rangeCells Value
            if (Scribe.mode != LoadSaveMode.Saving)
            {
                if (rangeTypeSeletion == -1)
                {
                    rangeCells = null;
                    rangeTypeSeletion = rangeTypeSelection;
                }

                rangeTypeSelection = rangeTypeSeletion;
            }

            AdjustPower();
            RefreshPowerStatus();
        }

        [Unsaved] private CompPowerTrader powerComp;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                powerForSpeed = 0;
                powerForRange = 0;
            }

            powerComp = parent.TryGetComp<CompPowerTrader>();
            AdjustPower();
            RefreshPowerStatus();
        }

        protected virtual void AdjustPower()
        {
            powerForSpeed = Mathf.Clamp(powerForSpeed, 0, MaxPowerForSpeed);

            powerForRange = Mathf.Clamp(powerForRange, 0, MaxPowerForRange);
        }

        public void RefreshPowerStatus()
        {
            if (powerComp != null)
            {
                powerComp.PowerOutput = -(float)ReflectionUtility.CompProperties_Power_basePowerConsumption.GetValue(powerComp.Props) -
                                        SupplyPowerForSpeed -
                                        SupplyPowerForRange -
                                        AdditionalPowerDrain;
            }
        }

        public virtual float GetSpeedFactor()
        {
            var f = 0f;
            if (MaxPowerForSpeed != 0)
            {
                f = (powerForSpeed) / (MaxPowerForSpeed);
            }

            return Mathf.Lerp(Props.floatrange_SpeedFactor.min, Props.floatrange_SpeedFactor.max, f);
        }

        public virtual float GetRange()
        {
            if (RangeSetting)
            {
                var f = (powerForRange) / (MaxPowerForRange);
                return Mathf.Lerp(Props.floatrange_Range.min, Props.floatrange_Range.max, f);
            }

            return 0f;
        }

        public virtual IEnumerable<IntVec3> GetRangeCells()
        {
            if (RangeSetting)
            {
                //While adding a inBounds Check here might seem like a good idea doing so creates a risk to miss bugs.
                //We currently use many base Game functions as a fallback. those do not check for Bounds.
                //We should consider to implement a Class dedicated to this Job (When his is done we shall reconsider each check made in this commit / #314)
                return RangeCells(parent.Position, RangeTypeRot, parent.def, GetRange()) /*.Where(c => c.InBounds(this.parent.Map))*/;
            }

            return null;
        }

        public override void PostDrawExtraSelectionOverlays()
        {
            base.PostDrawExtraSelectionOverlays();
            if (RangeSetting)
            {
                DrawRangeCells(CommonColors.instance);
            }
        }

        public virtual void DrawRangeCells(Color color)
        {
            var range = GetRange();
            GenDraw.DrawFieldEdges(GetRangeCells().ToList(), color);
        }

        public IEnumerable<IntVec3> RangeCells(IntVec3 center, Rot4 rot, ThingDef thingDef, float range)
        {
            if (rangeCells == null)
            {
                rangeCells = (IRangeCells)Activator.CreateInstance(Props.rangeType);
            }

            return (rangeCells as IRangeCells).RangeCells(center, rot, thingDef, range);
        }

        public IRangeCells[] rangeTypes = new IRangeCells[] { new CircleRange(), new FacingRectRange(), new RectRange() };
    }

    public class CompProperties_PowerWorkSetting : CompProperties, IXMLThingDescription
    {
        //speed
        public FloatRange floatrange_SpeedFactor;
        public float powerPerStepSpeed;
        public float powerStepFactor = 1;

        //Range
        public FloatRange floatrange_Range;
        public float powerPerStepRange;

        //Range Type Settings
        public bool allowManualRangeTypeChange = false;
        public Type rangeType;

        private IRangeCells propsRangeType => (IRangeCells)Activator.CreateInstance(rangeType);

        public CompProperties_PowerWorkSetting()
        {
            compClass = typeof(CompPowerWorkSetting);
        }

        public override void DrawGhost(IntVec3 center, Rot4 rot, ThingDef thingDef, Color ghostCol, AltitudeLayer drawAltitude, Thing thing = null)
        {
            if (floatrange_Range.Span > 0)
            {
                base.DrawGhost(center, rot, thingDef, ghostCol, drawAltitude, thing);
                var min = propsRangeType.RangeCells(center, rot, thingDef, floatrange_Range.min);
                var max = propsRangeType.RangeCells(center, rot, thingDef, floatrange_Range.max);
                min.Select(c => new { Cell = c, Color = CommonColors.blueprintMin })
                    .Concat(max.Select(c => new { Cell = c, Color = CommonColors.blueprintMax }))
                    .GroupBy(a => a.Color)
                    .ToList()
                    .ForEach(g => GenDraw.DrawFieldEdges(g.Select(a => a.Cell).ToList(), g.Key));

                var map = Find.CurrentMap;
                map.listerThings.ThingsOfDef(thingDef)
                    .Select(t => t.TryGetComp<CompPowerWorkSetting>())
                    .Where(c => c != null && c.RangeSetting)
                    .ToList()
                    .ForEach(c => c.DrawRangeCells(CommonColors.otherInstance));
            }
        }

        //https://stackoverflow.com/a/457708
        static bool IsSubclassOfRawGeneric(Type generic, Type toCheck)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }

                toCheck = toCheck.BaseType;
            }

            return false;
        }

        public string GetDescription(ThingDef def)
        {
            var helptext = "";
            string tempstr;

            var isOfTypeBuilding_BaseMachine = IsSubclassOfRawGeneric(typeof(AutoMachineTool.Building_Base<>), def.thingClass);
            var factor = isOfTypeBuilding_BaseMachine ? 10 : 1;

            if (floatrange_SpeedFactor.Span > 0)
            {
                tempstr = $"{floatrange_SpeedFactor.min * factor} - {floatrange_SpeedFactor.max * factor}";
            }
            else
            {
                tempstr = $"{floatrange_SpeedFactor.min * factor}";
            }

            //Single speed of 1 is not intersting
            if (tempstr != "1")
            {
                helptext += "PRF_UTD_CompProperties_PowerWorkSetting_Speed".Translate(tempstr);
                helptext += "\r\n";
            }

            if (floatrange_Range.Span > 0)
            {
                tempstr = $"{floatrange_Range.min} - {floatrange_Range.max}";
            }
            else
            {
                tempstr = $"{floatrange_Range.min}";
            }

            //static range of 1 or 0 is not relevant for display
            if (tempstr != "0" && tempstr != "1")
            {
                helptext += "PRF_UTD_CompProperties_PowerWorkSetting_Range".Translate(tempstr);
                helptext += "\r\n";

                helptext += "PRF_UTD_CompProperties_PowerWorkSetting_RangeType".Translate(propsRangeType.ToText());
                helptext += "\r\n";
            }

            if (allowManualRangeTypeChange) helptext += "PRF_UTD_CompProperties_PowerWorkSetting_RangeTypeChange".Translate() + "\r\n";
            return helptext;
        }
    }

    public interface IRangeCells
    {
        IEnumerable<IntVec3> RangeCells(IntVec3 center, Rot4 rot, ThingDef thingDef, float range);

        string ToText();

        bool NeedsRotate { get; }
    }

    public class CircleRange : IRangeCells
    {
        public IEnumerable<IntVec3> RangeCells(IntVec3 center, Rot4 rot, ThingDef thingDef, float range)
        {
            return GenRadial.RadialCellsAround(center, range + Mathf.Max(thingDef.size.x, thingDef.size.z) - 1, true);
        }

        public string ToText()
        {
            return "PRF_SettingsTab_RangeType_CircleRange".Translate();
        }

        public bool NeedsRotate => false;
    }

    public class FacingRectRange : IRangeCells
    {
        public IEnumerable<IntVec3> RangeCells(IntVec3 center, Rot4 rot, ThingDef thingDef, float range)
        {
            return Util.FacingRect(center, thingDef.size, rot, Mathf.RoundToInt(range));
        }

        public string ToText()
        {
            return "PRF_SettingsTab_RangeType_FacingRectRange".Translate();
        }

        public bool NeedsRotate => true;
    }

    public class RectRange : IRangeCells
    {
        public IEnumerable<IntVec3> RangeCells(IntVec3 center, Rot4 rot, ThingDef thingDef, float range)
        {
            var size = thingDef.size;
            Util.CounterAdjustForRotation(ref center, ref size, rot);

            var under = GenAdj.CellsOccupiedBy(center, rot, size).ToHashSet();
            return GenAdj.CellsOccupiedBy(center, rot, thingDef.size + new IntVec2(Mathf.RoundToInt(range) * 2, Mathf.RoundToInt(range) * 2)).Where(c => !under.Contains(c));
        }

        public string ToText()
        {
            return "PRF_SettingsTab_RangeType_RectRange".Translate();
        }

        public bool NeedsRotate => false;
    }
}