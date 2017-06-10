using System.Collections.Generic;
using System.Linq;
using Harmony;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(RecipeWorkerCounter), "CountProducts")]
    public static class FilteredProductCounter
    {
        [HarmonyPrefix]
        static bool Prefix(ref Bill_Production bill, ref int __result)
        {
            var billWithThingFilter = bill as IBillWithThingFilter;
            if (billWithThingFilter == null || !BillUtility_Detour.CanOutputBeFiltered(bill))
            {
                // Counting a Thing that is a resource or a bill we don't control.
                // Defer back to vanilla counting function.
                return true;
            }

            __result = 0;
            var productThingDef = bill.recipe.products.First().thingDef;

            if (productThingDef.Minifiable)
            {
                var minifiedThings = bill.Map.listerThings.ThingsInGroup(ThingRequestGroup.MinifiedThing);
                foreach (var thing in minifiedThings)
                {
                    var minifiedThing = (MinifiedThing)thing;
                    var innerThing = minifiedThing.InnerThing;
                    if (innerThing.def == productThingDef &&
                        DoesThingMatchFilter(billWithThingFilter, innerThing) &&
                        DoesThingMatchFilter(billWithThingFilter, minifiedThing))
                    {
                        __result++;
                    }
                }

                return false;
            }

            SpecialThingFilterWorker_NonDeadmansApparel nonDeadmansApparelFilter = null;
            if (!billWithThingFilter.GetAllowDeadmansApparel())
            {
                // We want to filter out corpse worn apparel
                nonDeadmansApparelFilter = new SpecialThingFilterWorker_NonDeadmansApparel();
                if (!nonDeadmansApparelFilter.CanEverMatch(productThingDef))
                    // Not apparel, don't bother checking
                    nonDeadmansApparelFilter = null;
            }

            var thingList = bill.Map.listerThings.ThingsOfDef(productThingDef);

            foreach (var thing in thingList)
            {
                if (!DoesThingMatchFilter(billWithThingFilter, thing))
                    continue;

                if (nonDeadmansApparelFilter != null && !nonDeadmansApparelFilter.Matches(thing))
                    continue;

                __result += thing.stackCount;
            }

            return false;
        }

        private static bool DoesThingMatchFilter(IBillWithThingFilter bill, Thing thing)
        {
            if (bill.GetUseInputFilter() && thing.Stuff != null)
            {
                var billRaw = (Bill) bill;
                if (billRaw.ingredientFilter != null)
                {
                    if (!billRaw.ingredientFilter.Allows(thing.Stuff))
                        return false;
                }
            }

            var filter = bill.GetOutputFilter();
            QualityCategory quality;
            if (filter.allowedQualitiesConfigurable && thing.TryGetQuality(out quality))
            {
                if (!filter.AllowedQualityLevels.Includes(quality))
                {
                    return false;
                }
            }

            if (!filter.allowedHitPointsConfigurable)
                return true;

            var thingHitPointsPercent = (float)thing.HitPoints / thing.MaxHitPoints;

            return filter.AllowedHitPointsPercents.IncludesEpsilon(thingHitPointsPercent);
        }
    }
}