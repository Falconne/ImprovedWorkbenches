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
                __result = new Bill_ProductionWithUftWithFilters(recipe);
            }
            else
            {
                Main.Instance.Logger.Warning("Returning Bill_ProductionWithFilters");
                __result = new Bill_ProductionWithFilters(recipe);
            }
        }

        private static bool CanOutputBeFiltered(RecipeDef recipe)
        {
            return recipe.products != null &&
                   recipe.products.Count > 0 &&
                   recipe.products.First().thingDef.BaseMarketValue > 0;
        }

        private static void SetDefaultFilter<T>(T bill, RecipeDef recipe) where T: class 
        {
            var thingDef = recipe.products.First().thingDef;
            bill.OutputFilter.SetDisallowAll();
            bill.OutputFilter.SetAllow(thingDef, true);
        }
    }
}