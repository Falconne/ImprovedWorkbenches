using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    public interface IBillWithThingFilter
    {
        ThingFilter GetOutputFilter();

        RecipeDef GetRecipeDef();
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

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look<ThingFilter>(ref _outputFilter, "outputFilter", new object[0]);
        }

        private ThingFilter _outputFilter = new ThingFilter();
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

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look<ThingFilter>(ref _outputFilter, "outputFilter", new object[0]);
        }

        private ThingFilter _outputFilter = new ThingFilter();
    }
}