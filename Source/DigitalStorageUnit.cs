using HarmonyLib;
using System;
using System.Reflection;
using DigitalStorageUnit.Common;
using DigitalStorageUnit.Common.HarmonyPatches;
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

            ConditionalPatchHelper.InitHarmony(HarmonyInstance);
            ConditionalPatchHelper.Patch_Reachability_CanReach.PatchHandler(true);
        }
        catch (Exception ex)
        {
            Log.Error("DigitalStorageUnit :: Caught exception: " + ex);
        }

        try
        {
            LoadModSupport();
        }
        catch (Exception ex)
        {
            Log.Error("DigitalStorageUnit :: LoadModSupport Caught exception: " + ex);
        }
    }

    //Mod Support
    private void LoadModSupport()
    {
        if (ModLister.HasActiveModWithName("RimFridge Updated"))
        {
            // if "Simple Utilities: Fridge" and "[KV] RimFridge" are loaded we use "Simple Utilities: Fridge" as it is faster
            if (!ModLister.HasActiveModWithName("Simple Utilities: Fridge"))
            {
                MethodBase RrimFridge_CompRefrigerator_CompTickRare = AccessTools.Method("RimFridge.CompRefrigerator:CompTickRare");

                if (RrimFridge_CompRefrigerator_CompTickRare != null)
                {
                    var postfix = typeof(Patch_CompRefrigerator_CompTickRare).GetMethod("Postfix");
                    HarmonyInstance.Patch(RrimFridge_CompRefrigerator_CompTickRare, null, new HarmonyMethod(postfix));
                    Log.Message("DigitalStorageUnit - added Support for Fridge DSU Power using RimFridge");
                }
                else
                {
                    Log.Warning("DigitalStorageUnit - Failed to add Support for Fridge DSU Power using RimFridge");
                }
            }
        }

        if (ModLister.HasActiveModWithName("Simple Utilities: Fridge"))
        {
            MethodBase SimpleFridge_FridgeUtility_Tick = null;
            var FridgeUtility = Type.GetType("SimpleFridge.FridgeUtility, SimpleUtilitiesFridge", false);
            if (FridgeUtility != null)
            {
                SimpleFridge_FridgeUtility_Tick = AccessTools.Method(FridgeUtility, "Tick");
            }

            if (SimpleFridge_FridgeUtility_Tick != null)
            {
                var postfix = typeof(Patch_FridgeUtility_Tick).GetMethod("Postfix");
                HarmonyInstance.Patch(SimpleFridge_FridgeUtility_Tick, null, new HarmonyMethod(postfix));

                Log.Message("DigitalStorageUnit - added Support for Fridge DSU Power using Simple Utilities: Fridge");
            }
            else
            {
                Log.Warning("DigitalStorageUnit - Failed to add Support for Fridge DSU Power using Simple Utilities: Fridge");
            }
        }
    }

    public Harmony HarmonyInstance { get; private set; }

    public override string SettingsCategory()
    {
        return "DigitalStorageUnit".Translate();
    }

    public override void WriteSettings()
    {
    }
}