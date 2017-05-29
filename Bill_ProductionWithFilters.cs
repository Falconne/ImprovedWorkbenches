using System;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    public class AllowDeadmansApparelWrapper : IExposable
    {
        public AllowDeadmansApparelWrapper(bool value = false)
        {
            RawValue = value;
        }

        public void ExposeData()
        {
            Scribe_Values.Look<bool>(ref RawValue, "allowDeadmansApparelWrapped", false);
        }

        public bool RawValue;
    }

    public interface IBillWithThingFilter
    {
        ThingFilter GetOutputFilter();

        RecipeDef GetRecipeDef();

        Map GetMap();

        AllowDeadmansApparelWrapper GetAllowDeadmansApparelWrapper();
    }

    public class Bill_ProductionWithFilters : Bill_Production, IBillWithThingFilter
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

        public AllowDeadmansApparelWrapper GetAllowDeadmansApparelWrapper()
        {
            return _allowDeadmansApparel;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look<ThingFilter>(ref _outputFilter, "outputFilter", new object[0]);
            Scribe_Deep.Look<AllowDeadmansApparelWrapper>(ref _allowDeadmansApparel, 
                "allowDeadmansApparel", false);
        }

        private ThingFilter _outputFilter = new ThingFilter();

        private AllowDeadmansApparelWrapper _allowDeadmansApparel = new AllowDeadmansApparelWrapper();
    }

    public class Bill_ProductionWithUftWithFilters : Bill_ProductionWithUft, IBillWithThingFilter
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

        public AllowDeadmansApparelWrapper GetAllowDeadmansApparelWrapper()
        {
            return _allowDeadmansApparel;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look<ThingFilter>(ref _outputFilter, "outputFilter", new object[0]);
            Scribe_Deep.Look<AllowDeadmansApparelWrapper>(ref _allowDeadmansApparel,
                "allowDeadmansApparel", false);
        }

        private ThingFilter _outputFilter = new ThingFilter();

        private AllowDeadmansApparelWrapper _allowDeadmansApparel = new AllowDeadmansApparelWrapper();
    }
}