using System.Linq;
using Harmony;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(BillUtility), "MakeNewBill")]
    public static class BillUtility_Detour
    {
        [HarmonyPrefix]
        public static bool MakeNewBill(ref Bill __result, ref RecipeDef recipe)
        {
            if (recipe.UsesUnfinishedThing)
            {
                var newBill = new Bill_ProductionWithUftWithFilters(recipe);
                __result = SetDefaultFilter(newBill);
            }
            else
            {
                var newBill = new Bill_ProductionWithFilters(recipe);
                __result = SetDefaultFilter(newBill);
            }

            return false;
        }

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