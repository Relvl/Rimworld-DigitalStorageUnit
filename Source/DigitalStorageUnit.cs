using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using DigitalStorageUnit._harmony;
using UnityEngine;
using Verse;

namespace DigitalStorageUnit;

// ReSharper disable once UnusedType.Global
[StaticConstructorOnStartup]
public class DigitalStorageUnit : Mod
{
    private static readonly string ModID = "Relvl.DigitalStorageUnit";
    public static readonly bool IsDeepStorage = ModsConfig.ActiveModsInLoadOrder.Any(m => "LWM.DeepStorage".EqualsIgnoreCase(m.packageIdLowerCase));

    public static DigitalStorageUnitConfig Config { get; private set; } = new();

    static DigitalStorageUnit()
    {
    }

    public DigitalStorageUnit(ModContentPack content) : base(content)
    {
        try
        {
            // Init the configs
            Config = GetSettings<DigitalStorageUnitConfig>();
            DigitalStorageUnitConfig.ModInstance = this;
            
            // Init the Harmony
            var harmonyInstance = new Harmony(ModID);
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

            if (IsDeepStorage)
            {
                try
                {
                    var dsPatchPostfix = AccessTools.Method("LWM.DeepStorage.Open_DS_Tab_On_Select:Postfix");
                    var dsuPatchPrefix = AccessTools.Method(nameof(Patch_Open_DS_Tab_On_Select_Postfix) + ":Prefix");
                    harmonyInstance.Patch(dsPatchPostfix, prefix: new HarmonyMethod(dsuPatchPrefix));
                    Log.Message("DigitalStorageUnit: LWM.DeepStorage.Open_DS_Tab_On_Select:Postfix patched");
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                }
            }

            // Okay now
            Log.Message($"DigitalStorageUnit {typeof(DigitalStorageUnit).Assembly.GetName().Version} - Harmony patches successful");
        }
        catch (Exception ex)
        {
            Log.Error("DigitalStorageUnit :: Caught exception: " + ex);
        }
    }

    public override void DoSettingsWindowContents(Rect inRect) => Config.DoSettingsWindowContents(inRect);

    public override string SettingsCategory() => "DSU".Translate();
}