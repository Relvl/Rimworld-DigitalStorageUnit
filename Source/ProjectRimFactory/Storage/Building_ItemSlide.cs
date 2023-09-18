using ProjectRimFactory.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace ProjectRimFactory.Storage
{
    class Building_ItemSlide : Building_Crate, IPRF_Building
    {
        public IEnumerable<Thing> AvailableThings => throw new NotImplementedException();

        public virtual bool ForbidOnPlacingDefault
        {
            get => forbidOnPlacingDefault;
            set => forbidOnPlacingDefault = value;
        }

        protected bool forbidOnPlacingDefault = false;

        public bool ObeysStorageFilters => false;

        public bool OutputToEntireStockpile => false;

        public bool AcceptsThing(Thing newThing, IPRF_Building giver = null)
        {
            if (Accepts(newThing))
            {
                Notify_ReceivedThing(newThing);
                return true;
            }

            return false;
        }

        public void EffectOnAcceptThing(Thing t)
        {
        }

        public void EffectOnPlaceThing(Thing t)
        {
        }

        //TODO
        public bool ForbidOnPlacing(Thing t)
        {
            if (outputCell.GetThingList(Map).Where(type => type is IPRF_Building || type is Building_StorageUnitIOBase || type is Building_MassStorageUnit).Any<Thing>())
            {
                return ForbidOnPlacingDefault;
            }

            return true;
        }

        public Thing GetThingBy(Func<Thing, bool> optionalValidator = null)
        {
            throw new NotImplementedException();
        }

        private IntVec3 outputCell => Position + Rotation.FacingCell;

        private void trySlideItem()
        {
            var target_IPRF_Building = (IPRF_Building)outputCell.GetThingList(Map).Where(type => type is IPRF_Building).FirstOrDefault<Thing>();
            if (target_IPRF_Building != null)
            {
                target_IPRF_Building.AcceptsThing(StoredItems[0], this);
            }
            else
            {
                this.PRFTryPlaceThing(StoredItems[0], outputCell, Map);
            }
        }

        public override void Tick()
        {
            base.Tick();
            if (!Spawned) return;

            if (StoredItemsCount > 0)
            {
                trySlideItem();
            }
        }
    }

    class PlaceWorker_ItemSlide : PlaceWorker
    {
        public override AcceptanceReport AllowsPlacing(BuildableDef checkingDef, IntVec3 loc, Rot4 rot, Map map, Thing thingToIgnore = null, Thing thing = null)
        {
            var acceptanceBase = base.AllowsPlacing(checkingDef, loc, rot, map, thingToIgnore, thing);
            if (acceptanceBase.Accepted)
            {
                //Check if the traget is another slide
                var outputCell = loc + rot.FacingCell;
                var thingList = map.thingGrid.ThingsListAt(outputCell);
                if (thingList.Where(t => t is Building_ItemSlide || t.def.entityDefToBuild == checkingDef).Any())
                    return new AcceptanceReport("PRF_PlaceWorker_ItemSlide_Denied".Translate());

                //Check if there is a slide placing there
                if (GenAdj.CellsAdjacentCardinal(loc, rot, checkingDef.Size)
                    .Where(
                        c => map.thingGrid.ThingsListAt(c)
                            .Where(t => (t is Building_ItemSlide || t.def.entityDefToBuild == checkingDef) && (t.Position + t.Rotation.FacingCell == loc))
                            .Any()
                    )
                    .Any())
                {
                    return new AcceptanceReport("PRF_PlaceWorker_ItemSlide_Denied".Translate());
                }
            }

            return acceptanceBase;
        }

        public override void DrawGhost(ThingDef def, IntVec3 center, Rot4 rot, Color ghostCol, Thing thing = null)
        {
            var outputCell = center + rot.FacingCell;

            GenDraw.DrawFieldEdges(new List<IntVec3> { outputCell }, CommonColors.outputCell);
        }
    }
}