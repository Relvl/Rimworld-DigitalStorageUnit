using HarmonyLib;
using System;
using System.Reflection;
using Verse;

namespace DigitalStorageUnit;

// ReSharper disable once UnusedType.Global
public class DigitalStorageUnit : Mod
{
    public DigitalStorageUnit(ModContentPack content) : base(content)
    {
        try
        {
            HarmonyInstance = new Harmony("io.github.Relvl.Rimworld.DigitalStorageUnit");
            HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
            Log.Message($"DigitalStorageUnit {typeof(DigitalStorageUnit).Assembly.GetName().Version} - Harmony patches successful");
        }
        catch (Exception ex)
        {
            Log.Error("DigitalStorageUnit :: Caught exception: " + ex);
        }
    }

    public Harmony HarmonyInstance { get; }

    public override string SettingsCategory()
    {
        return "DigitalStorageUnit".Translate();
    }

    public override void WriteSettings()
    {
    }
}