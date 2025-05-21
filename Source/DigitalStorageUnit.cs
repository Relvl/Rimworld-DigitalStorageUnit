using HarmonyLib;
using System;
using System.Linq;
using System.Reflection;
using DigitalStorageUnit._harmony;
using DigitalStorageUnit._harmony.rimfeller;
using UnityEngine;
using Verse;

namespace DigitalStorageUnit;

// ReSharper disable once UnusedType.Global
[StaticConstructorOnStartup]
public class DigitalStorageUnit : Mod
{
    private const string ModID = "Relvl.DigitalStorageUnit";

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

            // Init the Harmony
            var harmonyInstance = new Harmony(ModID);
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

            if (ModsConfig.ActiveModsInLoadOrder.Any(m => "LWM.DeepStorage".EqualsIgnoreCase(m.packageIdLowerCase)))
            {
                try
                {
                    var dsPatchPostfix = AccessTools.Method("LWM.DeepStorage.Open_DS_Tab_On_Select:Postfix");
                    // todo! wtf with this ":Prefis" when we're using a Postfix???
                    var dsuPatchPrefix = AccessTools.Method(nameof(SubPatch_Open_DS_Tab_On_Select_Postfix) + ":Prefix");
                    harmonyInstance.Patch(dsPatchPostfix, prefix: new HarmonyMethod(dsuPatchPrefix));
                    Log.Message("DigitalStorageUnit: LWM.DeepStorage.Open_DS_Tab_On_Select:Postfix patched");
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                }
            }

            if (ModsConfig.ActiveModsInLoadOrder.Any(m => "adaptive.storage.framework".EqualsIgnoreCase(m.packageIdLowerCase)))
            {
                try
                {
                    harmonyInstance.Patch(
                        AccessTools.Method("AdaptiveStorage.HarmonyPatches.AddITabToLegacyStorages:Postfix"),
                        prefix: AccessTools.Method(typeof(SubPatch_AdaptiveStorage_AddITabToLegacyStorages_Postfix), nameof(SubPatch_AdaptiveStorage_AddITabToLegacyStorages_Postfix.Prefix))
                    );
                }
                catch (Exception e)
                {
                    Log.Error(e.ToString());
                }
            }

            if (ModsConfig.ActiveModsInLoadOrder.Any(m => "Dubwise.Rimefeller".EqualsIgnoreCase(m.packageIdLowerCase)))
            {
                try
                {
                    harmonyInstance.Patch(
                        AccessTools.Method("Rimefeller.CompRefinery:get_AdjacentHoppers"),
                        prefix: AccessTools.Method(typeof(SubParch_Rimfeller_CompRefinery), nameof(SubParch_Rimfeller_CompRefinery.Prefix))
                    );
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