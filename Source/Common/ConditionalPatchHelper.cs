using HarmonyLib;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DigitalStorageUnit.Storage;
using Verse;

namespace DigitalStorageUnit.Common;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public static class ConditionalPatchHelper
{
    public class TogglePatch
    {
        public bool Status { get; private set; }

        private readonly MethodInfo _baseMethod;
        private readonly HarmonyMethod _transpilerHarmonyMethod;
        private readonly HarmonyMethod _prefixHarmonyMethod;
        private readonly HarmonyMethod _postfixHarmonyMethod;
        private readonly MethodInfo _transpilerMethod;
        private readonly MethodInfo _prefixMethod;
        private readonly MethodInfo _postfixMethod;

        public TogglePatch(MethodInfo baseMethod, MethodInfo prefix = null, MethodInfo postfix = null, MethodInfo transpiler = null)
        {
            _baseMethod = baseMethod;
            _transpilerMethod = transpiler;
            _prefixMethod = prefix;
            _postfixMethod = postfix;
            if (transpiler != null) _transpilerHarmonyMethod = new HarmonyMethod(transpiler);
            if (prefix != null) _prefixHarmonyMethod = new HarmonyMethod(prefix);
            if (postfix != null) _postfixHarmonyMethod = new HarmonyMethod(postfix);
        }

        public void PatchHandler(bool patch)
        {
            if (patch && !Status)
            {
                _harmonyInstance.Patch(_baseMethod, _prefixHarmonyMethod, _postfixHarmonyMethod, _transpilerHarmonyMethod);
                Status = true;
            }
            else if (Status && !patch)
            {
                if (_transpilerMethod != null) _harmonyInstance.Unpatch(_baseMethod, _transpilerMethod);
                if (_prefixMethod != null) _harmonyInstance.Unpatch(_baseMethod, _prefixMethod);
                if (_postfixMethod != null) _harmonyInstance.Unpatch(_baseMethod, _postfixMethod);
                Status = false;
            }
        }
    }

    private static Harmony _harmonyInstance;

    //Storage Patches

    public static readonly TogglePatch PatchBuildingStorageAccepts = new(
        AccessTools.Method(typeof(RimWorld.Building_Storage), "Accepts", new[] { typeof(Thing) }),
        AccessTools.Method(typeof(HarmonyPatches.Patch_Building_Storage_Accepts), "Prefix")
    );

    public static readonly TogglePatch PatchForbidUtilityIsForbidden = new(
        AccessTools.Method(typeof(RimWorld.ForbidUtility), "IsForbidden", new[] { typeof(Thing), typeof(Pawn) }),
        AccessTools.Method(typeof(HarmonyPatches.Patch_ForbidUtility_IsForbidden), "Prefix")
    );

    public static void InitHarmony(Harmony harmony) => _harmonyInstance = harmony;

    private static readonly List<Building_MassStorageUnit> BuildingMassStorages = new();

    private static void UpdatePatchStorage()
    {
        var state = BuildingMassStorages.Count > 0;

        PatchBuildingStorageAccepts.PatchHandler(state);
        PatchForbidUtilityIsForbidden.PatchHandler(state);
    }

    public static void Register(Building_MassStorageUnit building)
    {
        BuildingMassStorages.Add(building);
        UpdatePatchStorage();
    }

    public static void Deregister(Building_MassStorageUnit building)
    {
        BuildingMassStorages.Remove(building);
        UpdatePatchStorage();
    }
}