using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Common;

public interface IPowerSupplyMachineHolder
{
    IPowerSupplyMachine RangePowerSupplyMachine { get; }
}

public interface IPowerSupplyMachine
{
    float BasePowerConsumption { get; }
    int CurrentPowerConsumption { get; }

    //Strig will be formated for the overview and the value will hold the additional consumption
    Dictionary<string, int> AdditionalPowerConsumption { get; }

    int MaxPowerForSpeed { get; }
    int MaxPowerForRange { get; }

    float PowerPerStepSpeed { get; }
    float PowerPerStepRange { get; }

    FloatRange FloatRange_Range { get; }
    float CurrentRange { get; }

    FloatRange FloatRange_SpeedFactor { get; }
    float CurrentSpeedFactor { get; }

    float SupplyPowerForSpeed { get; set; }
    float SupplyPowerForRange { get; set; }

    bool Glowable { get; }
    bool Glow { get; set; }
    bool SpeedSetting { get; }
    bool RangeSetting { get; }

    void RefreshPowerStatus();
}

class ITab_PowerSupply : ITab
{
    private static readonly Vector2 WinSize = new(600f, 130f);

    private static readonly float HeightSpeed = 120 - 25;

    private static readonly float HeightRange = 100 - 25;

    private static readonly float HeightGlow = 30;

    public ITab_PowerSupply()
    {
        size = WinSize;
        labelKey = "PRF.AutoMachineTool.SupplyPower.TabName";

        descriptionForSpeed = "PRF.AutoMachineTool.SupplyPower.Description".Translate();
        descriptionForRange = "PRF.AutoMachineTool.SupplyPower.DescriptionForRange".Translate();
    }

    private string descriptionForSpeed;

    private string descriptionForRange;

    private IPowerSupplyMachine Machine => (SelThing as IPowerSupplyMachineHolder)?.RangePowerSupplyMachine;

    public override bool IsVisible => Machine != null && (Machine.SpeedSetting || Machine.RangeSetting);

    public override void TabUpdate()
    {
        base.TabUpdate();

        var additionalHeight = (Machine.SpeedSetting ? HeightSpeed : 0) + (Machine.RangeSetting ? HeightRange : 0) + (Machine.Glowable ? HeightGlow : 0);
        size = new Vector2(WinSize.x, WinSize.y + additionalHeight);
        UpdateSize();
    }

    public override void OnOpen()
    {
        base.OnOpen();
    }

    protected override void FillTab()
    {
        TextAnchor anchor;
        GameFont font;

        var list = new Listing_Standard();
        var inRect = new Rect(0f, 0f, size.x, size.y).ContractedBy(10f);

        list.Begin(inRect);

        list.Gap();
        var rect = new Rect();

        //Add Power usage Breackdown
        rect = list.GetRect(50f);
        //TODO Use string builder
        string powerUsageBreackdown;
        powerUsageBreackdown = "PRF.AutoMachineTool.SupplyPower.BreakDownLine_Start".Translate(
            Machine.BasePowerConsumption,
            Machine.SupplyPowerForSpeed,
            Machine.SupplyPowerForRange
        );
        //Add breackdown for additional Power usage if any
        if (Machine.AdditionalPowerConsumption != null && Machine.AdditionalPowerConsumption.Count > 0)
        {
            foreach (var pair in Machine.AdditionalPowerConsumption)
            {
                powerUsageBreackdown += "PRF.AutoMachineTool.SupplyPower.BreakDownLine_Append".Translate(pair.Key, pair.Value);
            }
        }

        //Display the Sum
        powerUsageBreackdown += "PRF.AutoMachineTool.SupplyPower.BreakDownLine_End".Translate(-1 * Machine.CurrentPowerConsumption);
        Widgets.Label(rect, powerUsageBreackdown);
        rect = list.GetRect(10f);
        Widgets.DrawLineHorizontal(rect.x, rect.y, WinSize.x);

        //----------------------------

        if (Machine.SpeedSetting)
        {
            var minPowerSpeed = 0;
            var maxPowerSpeed = Machine.MaxPowerForSpeed;

            string valueLabelForSpeed = "PRF.AutoMachineTool.SupplyPower.ValueLabelForSpeed".Translate(Machine.SupplyPowerForSpeed);

            // for speed
            rect = list.GetRect(30f);
            Widgets.Label(rect, descriptionForSpeed);
            list.Gap();

            rect = list.GetRect(20f);
            var speed = (int)Widgets.HorizontalSlider_NewTemp(
                rect,
                (float)Machine.SupplyPowerForSpeed,
                (float)minPowerSpeed,
                (float)maxPowerSpeed,
                true,
                valueLabelForSpeed,
                "PRF.AutoMachineTool.SupplyPower.wdLabel".Translate(minPowerSpeed),
                "PRF.AutoMachineTool.SupplyPower.wdLabel".Translate(maxPowerSpeed),
                Machine.PowerPerStepSpeed
            );
            Machine.SupplyPowerForSpeed = speed;
            //Add info Labels below
            rect = list.GetRect(30f);
            anchor = Text.Anchor;
            font = Text.Font;
            Text.Font = GameFont.Tiny;

            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(rect, "PRF.AutoMachineTool.SupplyPower.PercentLabel".Translate((Machine.FloatRange_SpeedFactor.min / Machine.FloatRange_SpeedFactor.min) * 100));
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(rect, "PRF.AutoMachineTool.SupplyPower.PercentLabel".Translate((Machine.FloatRange_SpeedFactor.max / Machine.FloatRange_SpeedFactor.min) * 100));
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(rect, "PRF.AutoMachineTool.SupplyPower.CurrentPercent".Translate((Machine.CurrentSpeedFactor / Machine.FloatRange_SpeedFactor.min) * 100));
            Text.Anchor = anchor;
            Text.Font = font;

            list.Gap();

            //Check if this.Machine.RangeSetting is active to place a Devider line
            if (Machine.RangeSetting)
            {
                rect = list.GetRect(10f);
                Widgets.DrawLineHorizontal(rect.x, rect.y, WinSize.x);
            }
        }

        if (Machine.RangeSetting)
        {
            var minPowerRange = 0;
            var maxPowerRange = Machine.MaxPowerForRange;

            string valueLabelForRange = "PRF.AutoMachineTool.SupplyPower.ValueLabelForRange".Translate(Machine.SupplyPowerForRange);

            // for range
            rect = list.GetRect(30f);
            Widgets.Label(rect, descriptionForRange);
            list.Gap();

            rect = list.GetRect(20f);
            var range = Widgets.HorizontalSlider_NewTemp(
                rect,
                (float)Machine.SupplyPowerForRange,
                (float)minPowerRange,
                (float)maxPowerRange,
                true,
                valueLabelForRange,
                "PRF.AutoMachineTool.SupplyPower.wdLabel".Translate(minPowerRange),
                "PRF.AutoMachineTool.SupplyPower.wdLabel".Translate(maxPowerRange),
                Machine.PowerPerStepRange
            );
            Machine.SupplyPowerForRange = range;
            //Add info Labels below
            rect = list.GetRect(30f);
            anchor = Text.Anchor;
            font = Text.Font;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(rect, "PRF.AutoMachineTool.SupplyPower.CellsLabel".Translate(Machine.FloatRange_Range.min));
            Text.Anchor = TextAnchor.UpperRight;
            Widgets.Label(rect, "PRF.AutoMachineTool.SupplyPower.CellsLabel".Translate(Machine.FloatRange_Range.max));
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(rect, "PRF.AutoMachineTool.SupplyPower.CurrentCellRadius".Translate(Machine.CurrentRange));
            Text.Anchor = anchor;
            Text.Font = font;
            list.Gap();
        }

        //TODO Maybe move this to the settings tab
        if (Machine.Glowable)
        {
            rect = list.GetRect(30f);
            var glow = Machine.Glow;
            Widgets.CheckboxLabeled(rect, "PRF.AutoMachineTool.SupplyPower.SunLampText".Translate(), ref glow);
            Machine.Glow = glow;
        }

        list.Gap();

        list.End();
    }
}