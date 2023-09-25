using System;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

public class OutputSettings : IExposable
{
    public string MinTooltip;
    public string MaxTooltip;
    public bool UseMin;
    public bool UseMax;
    public int Min;
    public int Max;

    public OutputSettings(string minTooltip, string maxTooltip)
    {
        MinTooltip = minTooltip;
        MaxTooltip = maxTooltip;
        UseMin = false;
        UseMax = false;
        Min = 0;
        Max = 75;
    }

    public void ExposeData()
    {
        Scribe_Values.Look(ref MinTooltip, "minTooltip");
        Scribe_Values.Look(ref MaxTooltip, "maxTooltip");
        Scribe_Values.Look(ref UseMin, "useMin");
        Scribe_Values.Look(ref UseMax, "useMax");
        Scribe_Values.Look(ref Min, "min");
        Scribe_Values.Look(ref Max, "max");
    }

    public bool SatisfiesMax(int stackCount, int stackLimit)
    {
        return CountNeededToReachMax(stackCount, stackLimit) > 0;
    }

    public bool SatisfiesMin(int stackCount) => !UseMin || stackCount >= Min;

    public int CountNeededToReachMax(int currentCount, int limit) => UseMax ? Math.Min(limit, Max) - currentCount : limit - currentCount;

    public void Copy(OutputSettings other)
    {
        other.MinTooltip = MinTooltip;
        other.MaxTooltip = MaxTooltip;
        other.UseMin = UseMin;
        other.UseMax = UseMax;
        other.Min = Min;
        other.Max = Max;
    }
}