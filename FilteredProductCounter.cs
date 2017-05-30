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
            var productThingDef = billWithThingFilter.GetRecipeDef().products.First().thingDef;
            if (!billWithThingFilter.GetAllowDeadmansApparel())
            {
                // We want to filter out corpse worn apparel
                nonDeadmansApparelFilter = new SpecialThingFilterWorker_NonDeadmansApparel();
                if (!nonDeadmansApparelFilter.CanEverMatch(productThingDef))
                    // Not apparel, don't bother checking
                    nonDeadmansApparelFilter = null;
                
            }
            // Filter code originally adapted from Fluffy's Colony Manager
            foreach (var thingDef in filter.AllowedThingDefs)
            {
                var thingList = map.listerThings.ThingsOfDef(thingDef);

                foreach (var thing in thingList)
                {
                    QualityCategory quality;
                    if (filter.allowedQualitiesConfigurable && thing.TryGetQuality(out quality))
                    {
                        if (!filter.AllowedQualityLevels.Includes(quality))
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }

                    var thingHitPointsPercent = (float) thing.HitPoints / thing.MaxHitPoints;

                    if (!filter.AllowedHitPointsPercents.IncludesEpsilon(thingHitPointsPercent))
                        continue;

                    if (nonDeadmansApparelFilter != null && !nonDeadmansApparelFilter.Matches(thing))
                        continue;

                    __result += thing.stackCount;
                }
            }

            return false;
        }
    }
}