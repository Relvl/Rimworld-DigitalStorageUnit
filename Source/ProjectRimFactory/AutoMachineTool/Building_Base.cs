using ProjectRimFactory.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    public enum WorkingState
    {
        Ready,
        Working,
        Placing
    }

    public interface IProductOutput
    {
        IntVec3 OutputCell();
    }

    public abstract class Building_Base<T> : PRF_Building, IProductOutput where T : Thing
    {
        private WorkingState state;
        private static HashSet<T> workingSet = new HashSet<T>();
        protected T working;
        protected List<Thing> products = new List<Thing>();
        private float totalWorkAmount;
        private int workStartTick;
        [Unsaved] private Effecter progressBar;
        [Unsaved] protected bool showProgressBar = true;
        [Unsaved] protected bool placeFirstAbsorb = false;
        [Unsaved] protected bool readyOnStart = false;
        [Unsaved] protected int startCheckIntervalTicks = 30;

        public CompOutputAdjustable compOutputAdjustable;

        public override void PostMake()
        {
            base.PostMake();
            compOutputAdjustable = GetComp<CompOutputAdjustable>();
        }

        private MapTickManager mapManager;

        public override Thing GetThingBy(Func<Thing, bool> optionalValidator = null)
        {
            foreach (var p in products)
            {
                if (optionalValidator == null || optionalValidator(p))
                {
                    products.Remove(p);
                    return p;
                }
            }

            return null;
        }

        protected WorkingState State
        {
            get => state;
            set
            {
                if (state != value)
                {
                    OnChangeState(state, value);
                    state = value;
                }
            }
        }

        protected T Working => working;

        protected MapTickManager MapManager => mapManager;

        protected virtual void OnChangeState(WorkingState before, WorkingState after)
        {
        }

        protected virtual void ClearActions()
        {
            MapManager.RemoveAfterAction(Ready);
            MapManager.RemoveAfterAction(Placing);
            MapManager.RemoveAfterAction(CheckWork);
            MapManager.RemoveAfterAction(StartWork);
            MapManager.RemoveAfterAction(FinishWork);
        }

        /// <summary>
        /// The mode to use for saving/loading `working` for this class.
        ///   Use LookMode.Deep if no one else saves the object (e.g. if
        ///   the object is not spawned). Use LookMode.Reference if some
        ///   other source also saves the item - a spawned item is saved
        ///   by the map.
        /// </summary>
        protected virtual LookMode WorkingLookMode => LookMode.Reference;

        protected virtual LookMode ProductsLookMode => LookMode.Deep;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look(ref state, "workingState", WorkingState.Ready);
            Scribe_Values.Look(ref totalWorkAmount, "totalWorkAmount", 0f);
            Scribe_Values.Look(ref workStartTick, "workStartTick", 0);
            Scribe_Collections.Look<Thing>(ref products, "products", ProductsLookMode);
            if (WorkingLookMode == LookMode.Deep)
            {
                Scribe_Deep.Look<T>(ref working, "working");
            }
            else if (WorkingLookMode == LookMode.Reference)
            {
                Scribe_References.Look<T>(ref working, "working");
            }
        }

        public override void PostMapInit()
        {
            base.PostMapInit();
            if (products == null)
                products = new List<Thing>();
            if (working == null && State == WorkingState.Working)
                ForceReady();
            if (products.Count == 0 && State == WorkingState.Placing)
                ForceReady();
        }

        protected static bool InWorking(T thing)
        {
            return workingSet.Contains(thing);
        }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            mapManager = map.GetComponent<MapTickManager>();
            compOutputAdjustable = GetComp<CompOutputAdjustable>();
            if (readyOnStart) // No check for respawning after load?
            {
                State = WorkingState.Ready;
                Reset();
                MapManager.AfterAction(Rand.Range(0, startCheckIntervalTicks), Ready);
            }
            else
            {
                if (State == WorkingState.Ready)
                {
                    MapManager.AfterAction(Rand.Range(0, startCheckIntervalTicks), Ready);
                }
                else if (State == WorkingState.Working)
                {
                    MapManager.NextAction(StartWork);
                }
                else if (State == WorkingState.Placing)
                {
                    MapManager.NextAction(Placing);
                }
            }
        }

        public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
        {
            Reset();
            ClearActions();
            base.DeSpawn(mode);
        }

        protected virtual bool IsActive()
        {
            if (Destroyed || !Spawned)
            {
                return false;
            }

            //Check if Output is in Bounds
            if (!OutputCell().InBounds(Map)) return false;

            return true;
        }

        protected virtual void Reset()
        {
            if (State != WorkingState.Ready)
            {
                products.ForEach(
                    t =>
                    {
                        if (t != null && !t.Destroyed)
                        {
                            if (t.Spawned)
                            {
                                t.DeSpawn();
                            }

                            GenPlace.TryPlaceThing(t, Position, Map, ThingPlaceMode.Near);
                        }
                    }
                );
            }

            CleanupWorkingEffect();
            State = WorkingState.Ready;
            totalWorkAmount = 0;
            workStartTick = 0;
            workingSet.Remove(working);
            working = null;
            products.Clear();
        }

        protected void ForceReady()
        {
            Reset();
            ClearActions();
            MapManager.NextAction(Ready);
        }

        protected virtual void CleanupWorkingEffect()
        {
            Option(progressBar).ForEach(e => e.Cleanup());
            progressBar = null;
        }

        protected virtual void CreateWorkingEffect()
        {
            CleanupWorkingEffect();
            if (showProgressBar)
            {
                Option(ProgressBarTarget())
                    .ForEach(
                        t =>
                        {
                            progressBar = DefDatabase<EffecterDef>.GetNamed("AutoMachineTool_Effect_ProgressBar").Spawn();
                            progressBar.EffectTick(ProgressBarTarget(), TargetInfo.Invalid);
                            ((MoteProgressBar2)((SubEffecter_ProgressBar)progressBar.children[0]).mote).progressGetter = () => (CurrentWorkAmount / totalWorkAmount);
                        }
                    );
            }
        }

        protected virtual TargetInfo ProgressBarTarget()
        {
            if (working.Spawned)
            {
                return working;
            }

            return this;
        }

        protected virtual void Ready()
        {
            if (State != WorkingState.Ready || !Spawned)
            {
                return;
            }

            if (!IsActive())
            {
                Reset();
                MapManager.AfterAction(30, Ready);
                return;
            }

            if (TryStartWorking(out working, out totalWorkAmount))
            {
                State = WorkingState.Working;
                workStartTick = Find.TickManager.TicksAbs;
                MapManager.NextAction(StartWork);
            }
            else
            {
                MapManager.AfterAction(startCheckIntervalTicks, Ready);
            }
        }

        private int CalcRemainTick()
        {
            if (float.IsInfinity(totalWorkAmount))
            {
                return int.MaxValue;
            }

            return Mathf.Max(1, Mathf.CeilToInt((totalWorkAmount - CurrentWorkAmount) / WorkAmountPerTick));
        }

        protected float CurrentWorkAmount => (Find.TickManager.TicksAbs - workStartTick) * WorkAmountPerTick;

        protected float WorkLeft => totalWorkAmount - CurrentWorkAmount;

        protected virtual void StartWork()
        {
            if (State != WorkingState.Working || !Spawned)
            {
                return;
            }

            if (!IsActive())
            {
                ForceReady();
                return;
            }

            CreateWorkingEffect();
            MapManager.AfterAction(30, CheckWork);
            if (!float.IsInfinity(totalWorkAmount))
            {
                MapManager.AfterAction(CalcRemainTick(), FinishWork);
            }
        }

        protected virtual void ForceStartWork(T working, float workAmount)
        {
            Reset();
            ClearActions();

            State = WorkingState.Working;
            this.working = working;
            totalWorkAmount = workAmount;
            workStartTick = Find.TickManager.TicksAbs;
            MapManager.NextAction(StartWork);
        }

        protected virtual void CheckWork()
        {
            if (State != WorkingState.Working || !Spawned)
            {
                return;
            }

            if (!IsActive())
            {
                ForceReady();
                return;
            }

            if (WorkInterruption(working))
            {
                ForceReady();
                return;
            }

            if (CurrentWorkAmount >= totalWorkAmount)
            {
                // 作業中に電力が変更されて終わってしまった場合、次TickでFinish呼び出し.
                // If the power is changed during work and it ends, 
                //     call Finish with the next tick.
                MapManager.NextAction(FinishWork);
            }
            else
            {
                MapManager.AfterAction(30, CheckWork);
            }
        }

        protected virtual void FinishWork()
        {
            if (State != WorkingState.Working || !Spawned)
            {
                return;
            }

            MapManager.RemoveAfterAction(CheckWork);
            MapManager.RemoveAfterAction(FinishWork);
            if (!IsActive())
            {
                ForceReady();
                return;
            }

            if (WorkInterruption(working))
            {
                ForceReady();
                return;
            }

            if (FinishWorking(working, out products))
            {
                State = WorkingState.Placing;
                CleanupWorkingEffect();
                working = null;
                if (products == null || products.Count == 0)
                {
                    Reset();
                    MapManager.NextAction(Ready);
                }
                else
                {
                    MapManager.NextAction(Placing);
                }
            }
            else
            {
                Reset();
                MapManager.NextAction(Ready);
            }
        }

        protected virtual void Placing()
        {
            if (State != WorkingState.Placing || !Spawned)
            {
                return;
            }

            if (!IsActive())
            {
                ForceReady();
                return;
            }

            if (PlaceProduct(ref products))
            {
                State = WorkingState.Ready;
                Reset();
                MapManager.NextAction(Ready);
            }
            else
            {
                // If we are still Placing, try again in 30
                if (State == WorkingState.Placing) MapManager.AfterAction(30, Placing);
            }
        }

        protected abstract float WorkAmountPerTick { get; }

        protected abstract bool WorkInterruption(T working);

        protected abstract bool TryStartWorking(out T target, out float workAmount);

        protected abstract bool FinishWorking(T working, out List<Thing> products);

        protected bool forcePlace = true;

        protected virtual bool PlaceProduct(ref List<Thing> products)
        {
            // Use Aggregate() to attempt to place each item in products
            //   Any unplaced products accumulate in the new List<Thing>,
            //   and stay in products.
            // Is there any reason this uses `ref List<Thing> products`
            //   instead of `this.products`?
            products = products.Aggregate(
                new List<Thing>(),
                (total, target) =>
                {
                    if (target.Spawned) target.DeSpawn();
                    if (this.PRFTryPlaceThing(target, OutputCell(), Map, forcePlace))
                    {
                        return total;
                    }

                    return total.Append(target);
                }
            );
            // if there are any left in products, we didn't place them all:
            return !this.products.Any();
        }

        public override IntVec3 OutputCell()
        {
            return FacingCell(Position, def.Size, Rotation.Opposite);
        }

        public override string GetInspectString()
        {
            var msg = base.GetInspectString();
            msg += "\n";
            switch (State)
            {
                case WorkingState.Working:
                    if (float.IsInfinity(totalWorkAmount))
                    {
                        msg += "PRF.AutoMachineTool.StatWorkingNotParam".Translate();
                    }
                    else
                    {
                        msg += "PRF.AutoMachineTool.StatWorking".Translate(
                            Mathf.RoundToInt(Math.Min(CurrentWorkAmount, totalWorkAmount)),
                            Mathf.RoundToInt(totalWorkAmount),
                            Mathf.RoundToInt(Mathf.Clamp01(CurrentWorkAmount / totalWorkAmount) * 100)
                        );
                    }

                    break;
                case WorkingState.Ready:
                    msg += "PRF.AutoMachineTool.StatReady".Translate();
                    break;
                case WorkingState.Placing:
                    if (products.Count != 1)
                        msg += "PRF.AutoMachineTool.StatPlacing".Translate(products.Count);
                    else
                        msg += "PRF.AutoMachineTool.PlacingSingle".Translate(products[0].Label);
                    break;
                default:
                    msg += State.ToString();
                    break;
            }

            return msg;
        }

        public void NortifyReceivable()
        {
            if (State == WorkingState.Placing && Spawned)
            {
                if (!MapManager.IsExecutingThisTick(Placing))
                {
                    MapManager.RemoveAfterAction(Placing);
                    MapManager.NextAction(Placing);
                }
            }
        }

        protected List<Thing> CreateThings(ThingDef def, int count)
        {
            var quot = count / def.stackLimit;
            var remain = count % def.stackLimit;
            return Enumerable.Range(0, quot + 1)
                .Select((c, i) => i == quot ? remain : def.stackLimit)
                .Select(
                    c =>
                    {
                        var p = ThingMaker.MakeThing(def, null);
                        p.stackCount = c;
                        return p;
                    }
                )
                .ToList();
        }
    }
}