using System.Linq;
using Harmony;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(BillUtility), "MakeNewBill")]
    public static class BillUtility_Detour
    {
        [HarmonyPostfix]
        public static void MakeNewBill(ref Bill __result, ref RecipeDef recipe)
        {
            if (!CanOutputBeFiltered(recipe))
                return;

            if (recipe.UsesUnfinishedThing)
            {
                Main.Instance.Logger.Warning("Returning Bill_ProductionWithUftWithFilters");
                var newBill = new Bill_ProductionWithUftWithFilters(recipe);
                if (SetDefaultFilter(newBill))
                    __result = newBill;
            }
            else
            {
                Main.Instance.Logger.Warning("Returning Bill_ProductionWithFilters");
                var newBill = new Bill_ProductionWithFilters(recipe);
                if (SetDefaultFilter(newBill))
                    __result = newBill;
            }
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

        private static bool SetDefaultFilter<T>(T bill) where T: IBillWithThingFilter 
        {
            var thingDef = bill.GetRecipeDef().products.First().thingDef;
            bill.GetOutputFilter().SetDisallowAll();
            bill.GetOutputFilter().SetAllow(thingDef, true);

            // If we can't filter on hit-points, ignore it completely
            return bill.GetOutputFilter().allowedHitPointsConfigurable;
        }
    }
}