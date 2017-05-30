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

        public ref bool GetAllowDeadmansApparel()
        {
            return ref _allowDeadmansApparel;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref _outputFilter, "outputFilter", new object[0]);
            Scribe_Values.Look(ref _allowDeadmansApparel, 
                "allowDeadmansApparel", false);
        }

        private ThingFilter _outputFilter = new ThingFilter();

        private bool _allowDeadmansApparel;
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

        public ref bool GetAllowDeadmansApparel()
        {
            return ref _allowDeadmansApparel;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref _outputFilter, "outputFilter", new object[0]);
            Scribe_Values.Look(ref _allowDeadmansApparel,
                "allowDeadmansApparel", false);
        }

        private ThingFilter _outputFilter = new ThingFilter();

        private bool _allowDeadmansApparel;
    }
}