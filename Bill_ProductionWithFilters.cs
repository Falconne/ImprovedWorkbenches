using System;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    public interface IBillWithThingFilter
    {
        ThingFilter GetOutputFilter();

        RecipeDef GetRecipeDef();

        Map GetMap();

        ref bool GetAllowDeadmansApparel();
    }

    public interface IBillWithWorkerFilter
    {
        Pawn GetWorker();

        void SetWorker(Pawn worker);

        IBillGiver GetBillGiver();
    }

    public class Bill_ProductionWithFilters : Bill_Production, IBillWithThingFilter, IBillWithWorkerFilter
    {
        public Bill_ProductionWithFilters()
        {
            
        }

        public Bill_ProductionWithFilters(RecipeDef recipe) : base(recipe)
        {
        }

        public ThingFilter GetOutputFilter()
        {
            return _outputFilter;
        }

        public RecipeDef GetRecipeDef()
        {
            return recipe;
        }

        public Map GetMap()
        {
            return Map;
        }

        public ref bool GetAllowDeadmansApparel()
        {
            return ref _allowDeadmansApparel;
        }

        public Pawn GetWorker()
        {
            return _worker;
        }

        public void SetWorker(Pawn worker)
        {
            _worker = worker;
        }

        public IBillGiver GetBillGiver()
        {
            return billStack?.billGiver;
        }


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref _outputFilter, "outputFilter", new object[0]);
            Scribe_Values.Look(ref _allowDeadmansApparel,
                "allowDeadmansApparel", false);
            Scribe_References.Look(ref _worker, "worker");
        }


        private ThingFilter _outputFilter = new ThingFilter();

        private bool _allowDeadmansApparel;
        private Pawn _worker;
    }

    public class Bill_ProductionWithUftWithFilters : 
        Bill_ProductionWithUft, IBillWithThingFilter, IBillWithWorkerFilter
    {
        public Bill_ProductionWithUftWithFilters()
        {
            
        }

        public Bill_ProductionWithUftWithFilters(RecipeDef recipe) : base(recipe)
        {
        }

        public ThingFilter GetOutputFilter()
        {
            return _outputFilter;
        }

        public RecipeDef GetRecipeDef()
        {
            return recipe;
        }

        public Map GetMap()
        {
            return Map;
        }

        public ref bool GetAllowDeadmansApparel()
        {
            return ref _allowDeadmansApparel;
        }

        public Pawn GetWorker()
        {
            return _worker;
        }

        public void SetWorker(Pawn worker)
        {
            _worker = worker;
        }

        public IBillGiver GetBillGiver()
        {
            return billStack?.billGiver;
        }


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref _outputFilter, "outputFilter", new object[0]);
            Scribe_Values.Look(ref _allowDeadmansApparel,
                "allowDeadmansApparel", false);
            Scribe_References.Look(ref _worker, "worker");
        }


        private ThingFilter _outputFilter = new ThingFilter();

        private bool _allowDeadmansApparel;
        private Pawn _worker;
    }
}