using ProjectRimFactory.Common;
using ProjectRimFactory.SAL3;
using ProjectRimFactory.SAL3.Tools;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    public abstract class SAL_TargetBench : IExposable
    {
        protected Building_AutoMachineTool mySAL;
        protected IntVec3 Position;
        protected Map map;
        protected Rot4 Rotation;

        //For IExposable
        public SAL_TargetBench()
        {
        }

        public SAL_TargetBench(Building_AutoMachineTool mySAL, IntVec3 position, Map map, Rot4 rotation)
        {
            this.mySAL = mySAL;
            Position = position;
            this.map = map;
            Rotation = rotation;
        }

        public abstract bool Ready();
        public abstract void Reserve();
        public abstract void Free();
        public abstract void WorkDone(out List<Thing> products);
        public abstract bool TryStartWork(out float workAmount);
        public abstract TargetInfo TargetInfo();

        public virtual void Reset(WorkingState workingState)
        {
        }

        public virtual void CreateWorkingEffect(MapTickManager mapTickManager)
        {
        }

        public virtual void CleanupWorkingEffect(MapTickManager mapTickManager)
        {
        }

        public void generalReserve(Building tb)
        {
            if (PRFGameComponent.PRF_StaticPawn == null) PRFGameComponent.GenStaticPawn();
            if (PRFGameComponent.PRF_StaticJob == null) PRFGameComponent.PRF_StaticJob = new Job(PRFDefOf.PRFStaticJob);

            //bool added = false;

            List<ReservationManager.Reservation> reservations;
            reservations = (List<ReservationManager.Reservation>)ReflectionUtility.sal_reservations.GetValue(map.reservationManager);
            var res = new ReservationManager.Reservation(PRFGameComponent.PRF_StaticPawn, PRFGameComponent.PRF_StaticJob, 1, -1, tb, null);

            if (!reservations.Where(r => r.Claimant == PRFGameComponent.PRF_StaticPawn && r.Job == PRFGameComponent.PRF_StaticJob && r.Target == tb).Any())
            {
                //Log.Message("pre adding:");
                //debugListReservations();
                //Log.Message($"added res for {res.Target}");
                reservations.Add(res);
                //added = true;
            }

            //Log.Message("pre reserve");
            //debugListReservations();
            ReflectionUtility.sal_reservations.SetValue(map.reservationManager, reservations);
            //Log.Message("post reserve");
            //debugListReservations();
            //if (added)
            //{
            //    Log.Message("post adding:");
            //    debugListReservations();
            //}
        }

        public void debugListReservations()
        {
            List<ReservationManager.Reservation> reservations;
            reservations = (List<ReservationManager.Reservation>)ReflectionUtility.sal_reservations.GetValue(map.reservationManager);
            reservations = reservations.Where(r => r.Faction != null && r.Faction.IsPlayer).ToList();
            foreach (var res in reservations)
            {
                Log.Message($"Reservation for {res.Claimant} at {res.Target}");
            }
        }

        public void generalRelease(Building tb)
        {
            if (PRFGameComponent.PRF_StaticPawn == null) PRFGameComponent.GenStaticPawn();
            PRFGameComponent.PRF_StaticJob ??= new Job(PRFDefOf.PRFStaticJob);

            if (tb is null)
            {
                Log.Warning("PRF generalRelease null target");
                return;
            }

            /*Log.Message("pre release");
            debugListReservations();
            */
            map.reservationManager.Release(tb, PRFGameComponent.PRF_StaticPawn, PRFGameComponent.PRF_StaticJob);
            //Log.Message("generalRelease for " + (Position + Rotation.FacingCell) );
            //Log.Message("post release");
            //debugListReservations();
        }

        public virtual void ExposeData()
        {
            Scribe_References.Look<Map>(ref map, "map");
            Scribe_References.Look<Building_AutoMachineTool>(ref mySAL, "mySAL");
            Scribe_Values.Look<IntVec3>(ref Position, "Position");
            Scribe_Values.Look<Rot4>(ref Rotation, "Rotation");
        }
    }

    public class SAL_TargetWorktable : SAL_TargetBench
    {
        public Building_WorkTable my_workTable;

        private Bill bill;
        private List<Thing> ingredients;
        private Thing dominant;
        private UnfinishedThing unfinished;

        [Unsaved] private Effecter workingEffect = null;
        [Unsaved] private Sustainer workingSound = null;

        //For IExposable
        public SAL_TargetWorktable()
        {
        }

        public SAL_TargetWorktable(Building_AutoMachineTool mySAL, IntVec3 position, Map map, Rot4 rotation, Building_WorkTable my_workTable) : base(mySAL, position, map, rotation)
        {
            this.my_workTable = my_workTable;
        }

        public override bool Ready()
        {
            if (my_workTable is null) return false;

            return !(!my_workTable.CurrentlyUsableForBills() || !my_workTable.billStack.AnyShouldDoNow);
        }

        public override void Free()
        {
            AllowBills();
            generalRelease(my_workTable);
        }

        public override void Reserve()
        {
            generalReserve(my_workTable);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look<UnfinishedThing>(ref unfinished, "unfinished");
            Scribe_References.Look<Bill>(ref bill, "bill");
            Scribe_References.Look<Thing>(ref dominant, "dominant");
            Scribe_References.Look<Building_WorkTable>(ref my_workTable, "my_workTable");
            if (unfinished == null)
            {
                Scribe_Collections.Look<Thing>(ref ingredients, "ingredients", LookMode.Deep);
            }
            else
            {
                Scribe_Collections.Look<Thing>(ref ingredients, "ingredients", LookMode.Reference);
            }
        }

        public interface IBill_PawnForbidded
        {
            Bill Original { get; set; }
        }

        /// <summary>
        /// Unforbid Bills by restoring them to the correct class Called after ForbidBills
        /// //May be removed once we think all/most old Saves have been converted 
        /// </summary>
        private void AllowBills()
        {
            if (my_workTable is null) return;
            if (my_workTable.BillStack.Bills.Any(b => b is IBill_PawnForbidded))
            {
                var tmp = my_workTable.BillStack.Bills.ToList();
                my_workTable.BillStack.Clear();
                my_workTable.BillStack.Bills.AddRange(
                    tmp.SelectMany(
                        b =>
                        {
                            var forbidded = b as IBill_PawnForbidded;
                            var unforbbided = b;
                            if (forbidded != null)
                            {
                                if (b is Bill_ProductionWithUft)
                                {
                                    unforbbided = ((Bill_ProductionWithUft)b).CopyTo(
                                        (Bill_ProductionWithUft)Activator.CreateInstance(forbidded.Original?.GetType() ?? typeof(Bill_ProductionWithUft), b.recipe, b.precept)
                                    );
                                }
                                else if (b is Bill_Production)
                                {
                                    unforbbided = ((Bill_Production)b).CopyTo(
                                        (Bill_Production)Activator.CreateInstance(forbidded.Original?.GetType() ?? typeof(Bill_Production), b.recipe, b.precept)
                                    );
                                }
                            }

                            return Option(unforbbided);
                        }
                    )
                );
            }
        }

        private List<Thing> Consumable()
        {
            return mySAL.GetAllTargetCells()
                .SelectMany(c => c.AllThingsInCellForUse(map, false)) // Use GatherThingsUtility to also grab from belts
                .Distinct<Thing>()
                .ToList();
        }

        public override void Reset(WorkingState workingState)
        {
            if (workingState == WorkingState.Working)
            {
                if (unfinished == null)
                {
                    ingredients.ForEach(t => GenPlace.TryPlaceThing(t, Position, map, ThingPlaceMode.Near));
                }
                else
                {
                    GenPlace.TryPlaceThing(unfinished, Position, map, ThingPlaceMode.Near);
                    unfinished.Destroy(DestroyMode.Cancel);
                }
            }

            bill = null;
            dominant = null;
            unfinished = null;
            ingredients = null;

            base.Reset(workingState);
        }

        public override void CreateWorkingEffect(MapTickManager mapTickManager)
        {
            workingEffect = bill.recipe.effectWorking?.Spawn();
            var sound = bill.recipe.soundWorking;

            if (sound?.sustain ?? false)
            {
                workingSound = sound?.TrySpawnSustainer(my_workTable);
                workingSound?.Maintain();
            }
            else if (sound != null)
            {
                sound.PlayOneShot(my_workTable);
            }

            mapTickManager.EachTickAction(EffectTick);

            base.CreateWorkingEffect(mapTickManager);
        }

        public override void CleanupWorkingEffect(MapTickManager mapTickManager)
        {
            workingEffect?.Cleanup();
            workingEffect = null;

            workingSound?.End();
            workingSound = null;

            mapTickManager.RemoveEachTickAction(EffectTick);
            base.CleanupWorkingEffect(mapTickManager);
        }

        protected bool EffectTick()
        {
            workingEffect?.EffectTick(new TargetInfo(mySAL), new TargetInfo(my_workTable));

            return workingEffect == null;
        }

        private struct ThingDefGroup
        {
            public ThingDef def;
            public List<ThingAmount> consumable;
        }

        /// <summary>
        /// I guess thet finds the correct ingridiants for the bill
        /// </summary>
        /// <param name="bill"></param>
        /// <param name="consumable"></param>
        /// <returns></returns>
        private List<ThingAmount> Ingredients(Bill bill, List<Thing> consumable)
        {
            var initial = consumable
                //                .Where(c => bill.IsFixedOrAllowedIngredient(c))
                .Select(x => new ThingAmount(x, x.stackCount))
                .ToList();

            Func<List<ThingAmount>, List<ThingDefGroup>> grouping = (consumableAmounts) => consumableAmounts.GroupBy(c => c.thing.def)
                .Select(c => new { Def = c.Key, Count = c.Sum(t => t.count), Amounts = c.Select(t => t) })
                .OrderByDescending(g => g.Def.IsStuff)
                .ThenByDescending(g => g.Count * bill.recipe.IngredientValueGetter.ValuePerUnitOf(g.Def))
                .Select(g => new ThingDefGroup() { def = g.Def, consumable = g.Amounts.ToList() })
                .ToList();

            var grouped = grouping(initial);

            var ingredients = bill.recipe.ingredients.Select(
                    i =>
                    {
                        var result = new List<ThingAmount>();
                        var remain = i.GetBaseCount();

                        foreach (var things in grouped)
                        {
                            foreach (var amount in things.consumable)
                            {
                                var thing = amount.thing;
                                if (i.filter.Allows(thing) &&
                                    (bill.ingredientFilter.Allows(thing) || i.IsFixedIngredient) &&
                                    !map.reservationManager.AllReservedThings().Contains(thing))
                                {
                                    remain = remain - bill.recipe.IngredientValueGetter.ValuePerUnitOf(thing.def) * amount.count;
                                    var consumption = amount.count;
                                    if (remain <= 0.0f)
                                    {
                                        consumption -= Mathf.RoundToInt(-remain / bill.recipe.IngredientValueGetter.ValuePerUnitOf(thing.def));
                                        remain = 0.0f;
                                    }

                                    result.Add(new ThingAmount(thing, consumption));
                                }

                                if (remain <= 0.0f)
                                    break;
                            }

                            if (remain <= 0.0f)
                                break;

                            if ((things.def.IsStuff && bill.recipe.productHasIngredientStuff) || !bill.recipe.allowMixingIngredients)
                            {
                                // ミックスしたり、stuffの場合には、一つの要求素材に複数種類のものを混ぜられない.
                                // なので、この種類では満たせなかったので、残りを戻して、中途半端に入った利用予定を空にする.
                                remain = i.GetBaseCount();
                                result.Clear();
                            }
                        }

                        if (remain <= 0.0f)
                        {
                            // 残りがなく、必要分が全て割り当てられれば、割り当てた分を減らして、その状態でソートして割り当て分を返す.
                            result.ForEach(
                                r =>
                                {
                                    var list = grouped.Find(x => x.def == r.thing.def).consumable;
                                    var c = list.Find(x => x.thing == r.thing);
                                    list.Remove(c);
                                    c.count = c.count - r.count;
                                    list.Add(c);
                                }
                            );
                            grouped = grouping(grouped.SelectMany(x => x.consumable).ToList());
                            return result;
                        }

                        // 割り当てできなければ、空リスト.
                        return new List<ThingAmount>();
                    }
                )
                .ToList();

            if (ingredients.All(x => x.Count > 0))
            {
                return ingredients.SelectMany(c => c).ToList();
            }

            return new List<ThingAmount>();
        }

        private Bill GetnextBill(List<Thing> consumable, out List<ThingAmount> ingredients)
        {
            ingredients = new List<ThingAmount>();
            //Return null as Workbench is not ready
            if (!my_workTable.CurrentlyUsableForBills()) return null;
            foreach (var bill in my_workTable.billStack)
            {
                //Ready to start?
                if (!bill.ShouldDoNow() || !bill.recipe.AvailableNow) continue;
                //Sufficiant skills?
                if (!bill.recipe.skillRequirements?.All(r => r.minLevel <= mySAL.GetSkillLevel(r.skill)) ?? false) continue;

                if (bill.recipe.ingredients.Count == 0)
                {
                    ingredients = null;
                    return bill;
                }

                if (consumable == null) continue;
                ingredients = Ingredients(bill, consumable);
                if (ingredients.Count > 0) return bill;
            }

            ingredients = new List<ThingAmount>();
            return null;
        }

        public override void WorkDone(out List<Thing> products)
        {
            products = GenRecipe2.MakeRecipeProducts(bill.recipe, mySAL, ingredients, dominant, my_workTable, bill.precept).ToList();

            ingredients.ForEach(i => bill.recipe.Worker.ConsumeIngredient(i, bill.recipe, map));
            Option(unfinished).ForEach(u => u.Destroy(DestroyMode.Vanish));
            bill.Notify_IterationCompleted(null, ingredients);

            bill = null;
            dominant = null;
            unfinished = null;
            ingredients = null;
            // Because we use custom GenRecipe2, we have to handle bonus items and product modifications directly:
            mySAL.ModifyProductExt?.ProcessProducts(products, this as IBillGiver, mySAL, bill.recipe); // this as IBillGiver is probably null
        }

        public override bool TryStartWork(out float workAmount)
        {
            var consumable = Consumable();

            List<ThingAmount> things;

            var nextbill = GetnextBill(consumable, out things);
            if (nextbill != null)
            {
                bill = nextbill;

                ingredients = things?.Where(t => t.count > 0).Select(t => t.thing.SplitOff(t.count))?.ToList() ?? new List<Thing>();

                //Get dominant ingredient
                dominant = ProjectSAL_Utilities.CalculateDominantIngredient(bill.recipe, ingredients);

                if (bill.recipe.UsesUnfinishedThing)
                {
                    var stuff = (!bill.recipe.unfinishedThingDef.MadeFromStuff) ? null : dominant.def;
                    unfinished = (UnfinishedThing)ThingMaker.MakeThing(bill.recipe.unfinishedThingDef, stuff);
                    unfinished.BoundBill = (Bill_ProductionWithUft)bill;
                    unfinished.ingredients = ingredients;
                    var compColorable = unfinished.TryGetComp<CompColorable>();
                    if (compColorable != null)
                    {
                        compColorable.SetColor(dominant.DrawColor);
                    }
                }

                ThingDef thingDef = null;
                if (bill.recipe.UsesUnfinishedThing && bill.recipe.unfinishedThingDef.MadeFromStuff)
                {
                    thingDef = bill.recipe.UsesUnfinishedThing ? dominant?.def : null;
                }

                workAmount = bill.recipe.WorkAmountTotal(thingDef);

                var speedfact = my_workTable.GetStatValue(StatDefOf.WorkTableWorkSpeedFactor);

                workAmount /= speedfact;
                return true;
            }

            workAmount = 0;
            return false;
        }

        public override TargetInfo TargetInfo()
        {
            return my_workTable;
        }

        /// <summary>
        /// Used to reseve the workbench
        /// </summary>
        public class Bill_ProductionPawnForbidded : Bill_Production, IBill_PawnForbidded
        {
            public Bill_ProductionPawnForbidded() : base()
            {
            }

            public Bill_ProductionPawnForbidded(RecipeDef recipe) : base(recipe)
            {
            }

            public override bool PawnAllowedToStartAnew(Pawn p)
            {
                return false;
            }

            public override void ExposeData()
            {
                base.ExposeData();
                Scribe_Deep.Look(ref original, "original");
                if (Scribe.mode == LoadSaveMode.PostLoadInit)
                {
                    original.billStack = billStack;
                }
            }

            public Bill original;

            public Bill Original
            {
                get => original;
                set => original = value;
            }

            public override Bill Clone()
            {
                var clone = (Bill_Production)original.Clone();
                return this.CopyTo(clone);
            }

            public override void Notify_DoBillStarted(Pawn billDoer)
            {
                base.Notify_DoBillStarted(billDoer);
                Option(original).ForEach(o => o.Notify_DoBillStarted(billDoer));
            }

            public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
            {
                base.Notify_IterationCompleted(billDoer, ingredients);
                Option(original).ForEach(o => o.Notify_IterationCompleted(billDoer, ingredients));
            }

            public override void Notify_PawnDidWork(Pawn p)
            {
                base.Notify_PawnDidWork(p);
                Option(original).ForEach(o => o.Notify_PawnDidWork(p));
            }

            // proxy call. override other properties and methods.
        }

        /// <summary>
        /// Used to reseve the workbench
        /// </summary>
        public class Bill_ProductionWithUftPawnForbidded : Bill_ProductionWithUft, IBill_PawnForbidded
        {
            public Bill_ProductionWithUftPawnForbidded() : base()
            {
            }

            public Bill_ProductionWithUftPawnForbidded(RecipeDef recipe) : base(recipe)
            {
            }

            public override bool PawnAllowedToStartAnew(Pawn p)
            {
                return false;
            }

            public override void ExposeData()
            {
                base.ExposeData();
                Scribe_Deep.Look(ref original, "original");
                if (Scribe.mode == LoadSaveMode.PostLoadInit)
                {
                    original.billStack = billStack;
                }
            }

            public Bill original;

            public Bill Original
            {
                get => original;
                set => original = value;
            }

            public override Bill Clone()
            {
                var clone = (Bill_ProductionWithUft)original.Clone();
                return this.CopyTo(clone);
            }

            public override void Notify_DoBillStarted(Pawn billDoer)
            {
                base.Notify_DoBillStarted(billDoer);
                Option(original).ForEach(o => o.Notify_DoBillStarted(billDoer));
            }

            public override void Notify_IterationCompleted(Pawn billDoer, List<Thing> ingredients)
            {
                base.Notify_IterationCompleted(billDoer, ingredients);
                Option(original).ForEach(o => o.Notify_IterationCompleted(billDoer, ingredients));
            }

            public override void Notify_PawnDidWork(Pawn p)
            {
                base.Notify_PawnDidWork(p);
                Option(original).ForEach(o => o.Notify_PawnDidWork(p));
            }

            // proxy call. override other properties and methods.
        }

        private class ThingAmount
        {
            public ThingAmount(Thing thing, int count)
            {
                this.thing = thing;
                this.count = count;
            }

            public Thing thing;

            public int count;
        }
    }

    public class SAL_TargetResearch : SAL_TargetBench
    {
        private Building_ResearchBench researchBench;

        //For IExposable
        public SAL_TargetResearch()
        {
        }

        public SAL_TargetResearch(Building_AutoMachineTool mySAL, IntVec3 position, Map map, Rot4 rotation, Building_ResearchBench researchBench) : base(
            mySAL,
            position,
            map,
            rotation
        )
        {
            this.researchBench = researchBench;
        }

        public override bool Ready()
        {
            return !(Find.ResearchManager.currentProj == null || !Find.ResearchManager.currentProj.CanBeResearchedAt(researchBench, false));
        }

        public override void Free()
        {
            generalRelease(researchBench);
        }

        public override void Reserve()
        {
            generalReserve(researchBench);
        }

        public override void WorkDone(out List<Thing> products)
        {
            products = new List<Thing>();
            if (Find.ResearchManager.currentProj != null)
            {
                var statValue = Mathf.Max(mySAL.powerWorkSetting.GetSpeedFactor() * (mySAL.GetSkillLevel(SkillDefOf.Intellectual) * 0.115f + 0.08f), 0.1f);
                statValue *= researchBench.GetStatValue(StatDefOf.ResearchSpeedFactor);

                statValue /= Find.ResearchManager.currentProj.CostFactor(Faction.OfPlayer.def.techLevel);
                //Tuned the factor to 1000 from 100
                statValue *= 1000;

                Find.ResearchManager.ResearchPerformed(statValue, null);
            }
        }

        public override bool TryStartWork(out float workAmount)
        {
            workAmount = 0;
            if (Find.ResearchManager.currentProj != null)
            {
                workAmount = 1000f;
                return true;
            }

            return false;
        }

        public override TargetInfo TargetInfo()
        {
            return researchBench;
        }

        public override void ExposeData()
        {
            //Log.Message($"SAL_TargetResearch: {Scribe.mode}");
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                //Log.Message("call Free");
                //debugListReservations();
                Free();
                //Log.Message("Free done");
                //debugListReservations();
            }

            base.ExposeData();
            Scribe_References.Look<Building_ResearchBench>(ref researchBench, "researchBench");

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                //Log.Message("----------");
                //debugListReservations();
                //Log.Message("----------");
                Reserve();
            }
        }
    }

    public class SAL_TargetDeepDrill : SAL_TargetBench
    {
        private Building drilltypeBuilding;
        private CompDeepDrill compDeepDrill;

        //Based Upon Vanilla but capped at 1 to reduce unessesary calculations
        private readonly float[] miningyieldfactors = { 0.6f, 0.7f, 0.8f, 0.85f, 0.9f, 0.925f, 0.95f, 0.975f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f };

        private const float DeepDrill_WorkAmount = 1000f;

        //For IExposable
        public SAL_TargetDeepDrill()
        {
        }

        public SAL_TargetDeepDrill(Building_AutoMachineTool mySAL, IntVec3 position, Map map, Rot4 rotation, Building drilltypeBuilding) : base(mySAL, position, map, rotation)
        {
            this.drilltypeBuilding = drilltypeBuilding;
            compDeepDrill ??= drilltypeBuilding.TryGetComp<CompDeepDrill>();
        }

        public override bool Ready()
        {
            return !(drilltypeBuilding.TryGetComp<CompDeepDrill>().CanDrillNow() == false || (drilltypeBuilding.GetComp<CompForbiddable>()?.Forbidden ?? false));
        }

        public override void Free()
        {
            generalRelease(drilltypeBuilding);
        }

        public override void Reserve()
        {
            generalReserve(drilltypeBuilding);
        }

        public override void WorkDone(out List<Thing> products)
        {
            products = new List<Thing>();
            // From my understanding this WorkDone is added each pawn.tick
            //We dont want this with reflection so i will use a multiplier instead --> DeepDrill_WorkAmount

            var compDeepDrill = drilltypeBuilding.TryGetComp<CompDeepDrill>();

            //Vanilla Mining Speed Calc may need an Update if Vanilla is Updated 
            var statValue = DeepDrill_WorkAmount * Mathf.Max(mySAL.powerWorkSetting.GetSpeedFactor() * (mySAL.GetSkillLevel(SkillDefOf.Mining) * 0.12f + 0.04f), 0.1f);

            ReflectionUtility.drill_portionProgress.SetValue(compDeepDrill, (float)ReflectionUtility.drill_portionProgress.GetValue(compDeepDrill) + statValue);
            ReflectionUtility.drill_portionYieldPct.SetValue(
                compDeepDrill,
                (float)ReflectionUtility.drill_portionYieldPct.GetValue(compDeepDrill) + statValue * miningyieldfactors[mySAL.GetSkillLevel(SkillDefOf.Mining)] / 10000f
            );
            ReflectionUtility.drill_lastUsedTick.SetValue(compDeepDrill, Find.TickManager.TicksGame);
            if ((float)ReflectionUtility.drill_portionProgress.GetValue(compDeepDrill) > 10000f)
            {
                ReflectionUtility.drill_TryProducePortion.Invoke(compDeepDrill, new object[] { ReflectionUtility.drill_portionYieldPct.GetValue(compDeepDrill), null });
                ReflectionUtility.drill_portionProgress.SetValue(compDeepDrill, 0);
                ReflectionUtility.drill_portionYieldPct.SetValue(compDeepDrill, 0);
            }
        }

        public override bool TryStartWork(out float workAmount)
        {
            workAmount = 0;
            compDeepDrill ??= drilltypeBuilding.TryGetComp<CompDeepDrill>();
            if (compDeepDrill.CanDrillNow())
            {
                workAmount = DeepDrill_WorkAmount;
                return true;
            }

            return false;
        }

        public override TargetInfo TargetInfo()
        {
            return drilltypeBuilding;
        }

        public override void ExposeData()
        {
            //Log.Message($"SAL_TargetDeepDrill: {Scribe.mode}");
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                //Log.Message("call Free");
                //debugListReservations();
                Free();
                //Log.Message("Free done");
                //debugListReservations();
            }

            base.ExposeData();
            Scribe_References.Look<Building>(ref drilltypeBuilding, "drilltypeBuilding");
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                Reserve();
                //    Log.Message("----------");
                //    debugListReservations();
                //    Log.Message("----------");
            }
        }
    }
}