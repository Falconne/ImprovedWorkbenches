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
                SetDefaultFilter(newBill);
                __result = newBill;
            }
            else
            {
                Main.Instance.Logger.Warning("Returning Bill_ProductionWithFilters");
                var newBill = new Bill_ProductionWithFilters(recipe);
                SetDefaultFilter(newBill);
                __result = newBill;
            }
        }

        private static bool CanOutputBeFiltered(RecipeDef recipe)
        {
            return recipe.products != null &&
                   recipe.products.Count > 0 &&
                   recipe.products.First().thingDef.BaseMarketValue > 0;
        }

        private static void SetDefaultFilter<T>(T bill) where T: IBillWithThingFilter 
        {
            var thingDef = bill.GetRecipeDef().products.First().thingDef;
            bill.GetOutputFilter().SetDisallowAll();
            bill.GetOutputFilter().SetAllow(thingDef, true);
        }
    }
}