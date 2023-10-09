using System;
using Verse;

// ReSharper disable once CheckNamespace
namespace DigitalStorageUnit;

public class OutputSettings : IExposable
{
    public readonly string MinTooltip = "DSU.Min.Desc";
    public readonly string MaxTooltip = "DSU.Max.Desc";
    public bool UseMin;
    public bool UseMax;
    public int Min;
    public int Max;

    public OutputSettings()
    {
        UseMin = false;
        UseMax = false;
        Min = 0;
        Max = 75;
    }

    public void ExposeData()
    {
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
        other.UseMin = UseMin;
        other.UseMax = UseMax;
        other.Min = Min;
        other.Max = Max;
    }
}