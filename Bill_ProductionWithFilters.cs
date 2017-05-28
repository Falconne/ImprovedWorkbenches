using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    public class Bill_ProductionWithFilters : Bill_Production
    {
        public Bill_ProductionWithFilters(RecipeDef recipe) : base(recipe)
        {
        }

        public ThingFilter OutputFilter = new ThingFilter();

        public bool UseOutputFilter = false;
    }

    public class Bill_ProductionWithUftWithFilters : Bill_ProductionWithUft
    {
        public Bill_ProductionWithUftWithFilters(RecipeDef recipe) : base(recipe)
        {
        }

        public ThingFilter OutputFilter = new ThingFilter();

        public bool UseOutputFilter = false;
    }
}