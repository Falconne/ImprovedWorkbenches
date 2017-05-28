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

        private readonly ThingFilter _outputFilter = new ThingFilter();
    }

    public class Bill_ProductionWithUftWithFilters : Bill_ProductionWithUft, IBillWithThingFilter
    {
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

        private readonly ThingFilter _outputFilter = new ThingFilter();
    }
}