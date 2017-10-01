using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches.Filtering
{
    public class StatFilterWrapper
    {
        private readonly ExtendedBillData _extendedBillData;

        private readonly bool _isHitpointsFilterNeeded;

        private readonly bool _isQualityFilterNeeded;

        public StatFilterWrapper(ExtendedBillData extendedBillData)
        {
            _extendedBillData = extendedBillData;

            _isHitpointsFilterNeeded = _extendedBillData.IsHitpointsFilteringNeeded();
            _isQualityFilterNeeded = _extendedBillData.IsQualityFilteringNeeded();
        }

        public bool IsAnyFilteringNeeded()
        {
            return _extendedBillData.UseInputFilter
                || _isHitpointsFilterNeeded
                || _isQualityFilterNeeded
                || _extendedBillData.UsesCountingStockpile();
        }

        public bool DoesThingMatchFilter(Bill_Production bill, Thing thing)
        {
            if (!IsThingInAppropriateStockpile(thing))
                return false;

            if (_extendedBillData.UseInputFilter
                && thing.Stuff != null
                && bill.ingredientFilter != null)
            {
                if (!bill.ingredientFilter.Allows(thing.Stuff))
                    return false;
            }

            var filter = _extendedBillData.OutputFilter;
            if (_isQualityFilterNeeded)
            {
                QualityCategory quality;
                if (filter.allowedQualitiesConfigurable && thing.TryGetQuality(out quality))
                {
                    if (!filter.AllowedQualityLevels.Includes(quality))
                    {
                        return false;
                    }
                }
            }

            if (!_isHitpointsFilterNeeded)
                return true;

            var thingHitPointsPercent = (float)thing.HitPoints / thing.MaxHitPoints;

            return filter.AllowedHitPointsPercents.IncludesEpsilon(thingHitPointsPercent);

        }

        private bool IsThingInAppropriateStockpile(Thing thing)
        {
            if (!_extendedBillData.UsesCountingStockpile())
                return true;

            return _extendedBillData.GetThingsInCountingStockpile().Any(heldThing => heldThing == thing);
        }
    }
}