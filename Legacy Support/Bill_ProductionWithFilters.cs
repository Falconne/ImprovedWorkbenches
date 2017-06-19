using System;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    // These classes are no longer used. They are only left here to support porting
    // data over from older saves. Data is now stored in ExtendedBillDataStorage.

    public interface IBillWithThingFilter
    {
        ThingFilter GetOutputFilter();

        ref bool GetAllowDeadmansApparel();

        ref bool GetUseInputFilter();
    }

    public interface IBillWithWorkerFilter
    {
        Pawn GetWorker();

        void SetWorker(Pawn worker);
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

        public ref bool GetAllowDeadmansApparel()
        {
            return ref _allowDeadmansApparel;
        }

        public ref bool GetUseInputFilter()
        {
            return ref _useInputFilter;
        }

        public Pawn GetWorker()
        {
            return _worker;
        }

        public void SetWorker(Pawn worker)
        {
            _worker = worker;
        }


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref _outputFilter, "outputFilter", new object[0]);
            Scribe_Values.Look(ref _allowDeadmansApparel,
                "allowDeadmansApparel", false);
            Scribe_Values.Look(ref _useInputFilter,
                "useInputFilter", false);
            Scribe_References.Look(ref _worker, "worker");
        }


        private ThingFilter _outputFilter = new ThingFilter();

        private bool _allowDeadmansApparel;
        private bool _useInputFilter;
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

        public ref bool GetAllowDeadmansApparel()
        {
            return ref _allowDeadmansApparel;
        }

        public ref bool GetUseInputFilter()
        {
            return ref _useInputFilter;
        }

        public Pawn GetWorker()
        {
            return _worker;
        }

        public void SetWorker(Pawn worker)
        {
            _worker = worker;
        }


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref _outputFilter, "outputFilter", new object[0]);
            Scribe_Values.Look(ref _allowDeadmansApparel,
                "allowDeadmansApparel", false);
            Scribe_Values.Look(ref _useInputFilter,
                "useInputFilter", false);
            Scribe_References.Look(ref _worker, "worker");
        }


        private ThingFilter _outputFilter = new ThingFilter();

        private bool _allowDeadmansApparel;
        private bool _useInputFilter;
        private Pawn _worker;
    }
}