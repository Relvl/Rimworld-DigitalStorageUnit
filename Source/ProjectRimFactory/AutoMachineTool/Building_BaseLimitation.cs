using ProjectRimFactory.Common;
using RimWorld;
using System.Linq;
using Verse;
using static ProjectRimFactory.AutoMachineTool.Ops;

namespace ProjectRimFactory.AutoMachineTool
{
    public abstract class Building_BaseLimitation<T> : Building_BaseMachine<T>, IProductLimitation where T : Thing
    {
        public int ProductLimitCount
        {
            get => productLimitCount;
            set => productLimitCount = value;
        }

        public bool ProductLimitation
        {
            get => productLimitation;
            set => productLimitation = value;
        }

        private SlotGroup targetSlotGroup = null;

        public SlotGroup TargetSlotGroup
        {
            get => targetSlotGroup;
            set => targetSlotGroup = value;
        }

        public bool CountStacks
        {
            get => countStacks;
            set => countStacks = value;
        }

        public virtual bool ProductLimitationDisable => false;

        private int productLimitCount = 100;
        private bool productLimitation = false;
        private bool countStacks = false;

        private ILoadReferenceable slotGroupParent = null;
        private string slotGroupParentLabel = null;

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Values.Look<int>(ref productLimitCount, "productLimitCount", 100);
            Scribe_Values.Look<bool>(ref productLimitation, "productLimitation", false);
            Scribe_Values.Look<bool>(ref countStacks, "countStacks", false);

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                slotGroupParentLabel = targetSlotGroup?.parent?.SlotYielderLabel();
                slotGroupParent = targetSlotGroup?.parent as ILoadReferenceable;
            }

            Scribe_References.Look<ILoadReferenceable>(ref slotGroupParent, "slotGroupParent");
            Scribe_Values.Look<string>(ref slotGroupParentLabel, "slotGroupParentLabel", null);
        }

        public override void PostMapInit()
        {
            //Maybe rewrite that
            //From my understanding this gets that saved slot group
            targetSlotGroup = Map.haulDestinationManager.AllGroups.Where(g => g.parent.SlotYielderLabel() == slotGroupParentLabel)
                .Where(g => Option(slotGroupParent).Fold(true)(p => p == g.parent))
                .FirstOption()
                .Value;
            base.PostMapInit();
        }

        /* Use IsLimit(Thing thing) below
        [Obsolete("Warning, using IsLimit(ThingDef def) instead of (Thing t) does not work with all storage mods.")]
        public bool IsLimit(ThingDef def)
        {
            if (!this.ProductLimitation)
            {
                return false;
            }
            this.targetSlotGroup = this.targetSlotGroup.Where(s => this.Map.haulDestinationManager.AllGroups.Contains(s));
            return this.targetSlotGroup.Fold(() => this.CountFromMap(def) >= this.ProductLimitCount) // no slotGroup
                (s => !s.Settings.filter.Allows(def)
                || this.CountFromSlot(s, def) >= this.ProductLimitCount 
                || !s.CellsList.Any(c => c.GetFirstItem(this.Map) == null 
                || c.GetFirstItem(this.Map).def == def)); // this is broken anyway.  What if it's a full stack?
        }
        */

        // TODO: This may need to be cached somehow! (possibly by map?)
        // returns true if there IS something that limits adding this thing to storage.
        public bool IsLimit(Thing thing)
        {
            if (!productLimitation) return false;

            var targetSG = targetSlotGroup;

            if (targetSG == null)
            {
                return CountFromMap(thing.def) >= productLimitCount;
            }

            if (targetSG.parent is ILimitWatcher limitWatcher)
            {
                return (limitWatcher.ItemIsLimit(thing.def, countStacks, productLimitCount) || !targetSG.CellsList.Any(c => c.IsValidStorageFor(Map, thing)));
            }

            return (CheckSlotGroup(targetSG, thing.def, productLimitCount) || !targetSG.CellsList.Any(c => c.IsValidStorageFor(Map, thing)));
        }

        private int CountFromMap(ThingDef def)
        {
            return countStacks ? Map.listerThings.ThingsOfDef(def).Count : Map.resourceCounter.GetCount(def);
        }

        private bool CheckSlotGroup(SlotGroup s, ThingDef def, int Limit = int.MaxValue)
        {
            var count = 0;
            var Held = s.HeldThings;

            foreach (var t in Held)
            {
                if (t.def == def)
                {
                    if (countStacks)
                    {
                        count++;
                    }
                    else
                    {
                        count += t.stackCount;
                    }

                    if (count >= Limit) return true;
                }
            }

            return false;
        }
    }
}