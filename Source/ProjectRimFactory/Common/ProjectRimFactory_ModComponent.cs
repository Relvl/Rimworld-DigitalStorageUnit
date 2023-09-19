using HarmonyLib;
using ProjectRimFactory.Common.HarmonyPatches;
using System;
using System.Reflection;
using Verse;

namespace ProjectRimFactory.Common;

// ReSharper disable once UnusedType.Global
public class ProjectRimFactory_ModComponent : Mod
{
    public ProjectRimFactory_ModComponent(ModContentPack content) : base(content)
    {
        try
        {
            HarmonyInstance = new Harmony("com.spdskatr.projectrimfactory");
            HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
            
            Log.Message($"Project RimFactory Core {typeof(ProjectRimFactory_ModComponent).Assembly.GetName().Version} - Harmony patches successful");
            
            ConditionalPatchHelper.InitHarmony(HarmonyInstance);
            ConditionalPatchHelper.Patch_Reachability_CanReach.PatchHandler(true);
        }
        catch (Exception ex)
        {
            Log.Error("Project RimFactory Core :: Caught exception: " + ex);
        }

        try
        {
            LoadModSupport();
        }
        catch (Exception ex)
        {
            Log.Error("Project RimFactory Core :: LoadModSupport Caught exception: " + ex);
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
                    Log.Message("Project Rimfactory - added Support for Fridge DSU Power using RimFridge");
                }
                else
                {
                    Log.Warning("Project Rimfactory - Failed to add Support for Fridge DSU Power using RimFridge");
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

                Log.Message("Project Rimfactory - added Support for Fridge DSU Power using Simple Utilities: Fridge");
            }
            else
            {
                Log.Warning("Project Rimfactory - Failed to add Support for Fridge DSU Power using Simple Utilities: Fridge");
            }
        }
    }

    public Harmony HarmonyInstance { get; private set; }

    public override string SettingsCategory()
    {
        return "ProjectRimFactoryModName".Translate();
    }

    public override void WriteSettings()
    {
    }
}