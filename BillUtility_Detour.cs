using Harmony;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    public static class BillUtility_Detour
    {
        public static Bill MakeNewBill(this RecipeDef recipe)
        {
            if (recipe.UsesUnfinishedThing)
            {
                return new Bill_ProductionWithUftWithFilters(recipe);
            }

            return new Bill_ProductionWithFilters(recipe);
        }
    }
}