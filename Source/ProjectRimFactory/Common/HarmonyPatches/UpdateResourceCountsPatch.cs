﻿using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using Verse;

namespace ProjectRimFactory.Common.HarmonyPatches
{
    public interface IAssemblerQueue
    {
        Map Map { get; }
        List<Thing> GetThingQueue();
    }

    [HarmonyPatch(typeof(ResourceCounter), "UpdateResourceCounts")]
    class Patch_UpdateResourceCounts_AssemblerQueue
    {
        static void Postfix(ResourceCounter __instance, Dictionary<ThingDef, int> ___countedAmounts, Map ___map)
        {
            var i = 0;
            var gamecomp = Current.Game.GetComponent<PRFGameComponent>();
            for (i = 0; i < gamecomp.AssemblerQueue.Count; i++)
            {
                //Don't count Recorces of other maps
                if (gamecomp.AssemblerQueue[i].Map != ___map) continue;

                foreach (var heldThing in gamecomp.AssemblerQueue[i].GetThingQueue())
                {
                    var innerIfMinified = heldThing.GetInnerIfMinified();
                    //Added Should Count Checks
                    //EverStorable is form HeldThings
                    //Fresh Check is from ShouldCount (maybe we can hit that via harmony/reflection somhow)
                    if (innerIfMinified.def.EverStorable(false) && !innerIfMinified.IsNotFresh())
                    {
                        //Causes an error otherwise #345 (seems to be clothing that causes it)
                        if (___countedAmounts.ContainsKey(innerIfMinified.def))
                        {
                            ___countedAmounts[innerIfMinified.def] += innerIfMinified.stackCount;
                        }
                    }
                }
            }
        }
    }
}