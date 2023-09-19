using RimWorld;
using System.Reflection;

namespace ProjectRimFactory.SAL3;

public static class ReflectionUtility
{
    //basePowerConsumption
    public static readonly FieldInfo CompProperties_Power_basePowerConsumption =
        typeof(CompProperties_Power).GetField("basePowerConsumption", BindingFlags.Instance | BindingFlags.NonPublic);
}