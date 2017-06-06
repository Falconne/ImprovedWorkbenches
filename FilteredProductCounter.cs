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
            if (billWithThingFilter == null)
            {
                // Counting a Thing that is a resource or is otherwise lacking hit-points.
                // Defer back to vanilla counting function.
                return true;
            }

            var filter = billWithThingFilter.GetOutputFilter();
            var map = billWithThingFilter.GetMap();
            __result = 0;

            SpecialThingFilterWorker_NonDeadmansApparel nonDeadmansApparelFilter = null;
            var product = billWithThingFilter.GetRecipeDef().products.First();
            var productThingDef = product.thingDef;
            if (!billWithThingFilter.GetAllowDeadmansApparel())
            {
                // We want to filter out corpse worn apparel
                nonDeadmansApparelFilter = new SpecialThingFilterWorker_NonDeadmansApparel();
                if (!nonDeadmansApparelFilter.CanEverMatch(productThingDef))
                    // Not apparel, don't bother checking
                    nonDeadmansApparelFilter = null;
            }

            var thingList = map.listerThings.ThingsOfDef(productThingDef);

            foreach (var thing in thingList)
            {
                if (!DoesThingMatchFilter(filter, thing))
                    continue;

                if (nonDeadmansApparelFilter != null && !nonDeadmansApparelFilter.Matches(thing))
                    continue;

                __result += thing.stackCount;
            }

            if (!productThingDef.Minifiable)
                return false;

            var minifiedThings = bill.Map.listerThings.ThingsInGroup(ThingRequestGroup.MinifiedThing);
            foreach (var thing in minifiedThings)
            {
                var minifiedThing = (MinifiedThing) thing;
                var innerThing = minifiedThing.InnerThing;
                if (innerThing.def == productThingDef &&
                    DoesThingMatchFilter(filter, innerThing) &&
                    DoesThingMatchFilter(filter, minifiedThing))
                {
                    __result++;
                }
            }


            return false;
        }

        private static bool DoesThingMatchFilter(ThingFilter filter, Thing thing)
        {
            QualityCategory quality;
            if (filter.allowedQualitiesConfigurable && thing.TryGetQuality(out quality))
            {
                if (!filter.AllowedQualityLevels.Includes(quality))
                {
                    return false;
                }
            }

            var thingHitPointsPercent = (float) thing.HitPoints / thing.MaxHitPoints;

            return filter.AllowedHitPointsPercents.IncludesEpsilon(thingHitPointsPercent);
        }
    }
}