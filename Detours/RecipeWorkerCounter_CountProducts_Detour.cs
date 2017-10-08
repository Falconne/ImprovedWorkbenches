using System.Collections.Generic;
using System.Linq;
using Harmony;
using ImprovedWorkbenches.Filtering;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(RecipeWorkerCounter), "CountProducts")]
    public static class RecipeWorkerCounter_CountProducts_Detour
    {
        [HarmonyPrefix]
        static bool Prefix(ref Bill_Production bill, ref int __result)
        {
            if (!ExtendedBillDataStorage.CanOutputBeFiltered(bill))
                return true;

            var extendedBillData = Main.Instance.GetExtendedBillDataStorage().GetExtendedDataFor(bill);
            if (extendedBillData == null)
                return true;

            var productThingDef = bill.recipe.products.First().thingDef;
            var isThingAResource = productThingDef.CountAsResource;
            if (isThingAResource && !extendedBillData.UsesCountingStockpile())
                return true;

            var statFilterWrapper = new StatFilterWrapper(extendedBillData);

            if (!statFilterWrapper.IsAnyFilteringNeeded(productThingDef))
                return true;

            SpecialThingFilterWorker_NonDeadmansApparel nonDeadmansApparelFilter = null;
            if (!extendedBillData.AllowDeadmansApparel && productThingDef.IsApparel)
            {
                // We want to filter out corpse worn apparel
                nonDeadmansApparelFilter = new SpecialThingFilterWorker_NonDeadmansApparel();
                if (!nonDeadmansApparelFilter.CanEverMatch(productThingDef))
                    // Not apparel, don't bother checking
                    nonDeadmansApparelFilter = null;
            }


            __result = 0;
            if (productThingDef.Minifiable)
            {
                var minifiedThings = bill.Map.listerThings.ThingsInGroup(ThingRequestGroup.MinifiedThing);
                foreach (var thing in minifiedThings)
                {
                    var minifiedThing = (MinifiedThing)thing;
                    var innerThing = minifiedThing.InnerThing;
                    if (innerThing.def == productThingDef &&
                        statFilterWrapper.DoesThingMatchFilter(bill.ingredientFilter, innerThing) &&
                        statFilterWrapper.DoesThingMatchFilter(bill.ingredientFilter, minifiedThing))
                    {
                        __result++;
                    }
                }

                return false;
            }

            var thingList = bill.Map.listerThings.ThingsOfDef(productThingDef).ToList();
            if (statFilterWrapper.ShouldCheckWornClothes(productThingDef))
            {
                foreach (var colonist in Find.VisibleMap.mapPawns.FreeColonists)
                {
                    foreach (var apparel in colonist.apparel.WornApparel)
                    {
                        if (apparel.def == productThingDef)
                        {
                            thingList.Add(apparel);
                        }
                    }
                }
            }

            foreach (var thing in thingList)
            {
                if (!statFilterWrapper.DoesThingMatchFilter(bill.ingredientFilter, thing))
                    continue;

                if (nonDeadmansApparelFilter != null && !nonDeadmansApparelFilter.Matches(thing))
                    continue;

                __result += thing.stackCount;
            }

            return false;
        }
    }
}