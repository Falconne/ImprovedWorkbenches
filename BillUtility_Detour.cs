using System.Linq;
using Harmony;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    public static class BillUtility_Detour
    {
        internal static bool CanOutputBeFiltered(Bill_Production bill)
        {
            return CanOutputBeFiltered(bill.recipe);
        }

        // Figure out if output of recipe produces a "thing" with hit-points
        private static bool CanOutputBeFiltered(RecipeDef recipe)
        {
            if (recipe.products == null || recipe.products.Count == 0)
                return false;

            var thingDef = recipe.products.First().thingDef;
            if (thingDef.BaseMarketValue <= 0)
                return false;

            return !thingDef.CountAsResource;
        }

        private static T SetDefaultFilter<T>(T bill) where T : Bill_Production, IBillWithThingFilter
        {
            if (!CanOutputBeFiltered(bill))
                return bill;

            var thingDef = bill.recipe.products.First().thingDef;
            bill.GetOutputFilter().SetDisallowAll();
            bill.GetOutputFilter().SetAllow(thingDef, true);

            return bill;
        }
    }
}