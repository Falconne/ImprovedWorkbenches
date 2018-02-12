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

        public bool IsAnyFilteringNeeded(ThingDef thingDef)
        {
            return _extendedBillData.UseInputFilter
                || _isHitpointsFilterNeeded
                || _isQualityFilterNeeded
                || _extendedBillData.UsesCountingStockpile()
                || ShouldCheckWornClothes(thingDef)
                || ShouldCheckEquippedWeapons(thingDef)
                || (thingDef.IsApparel && !_extendedBillData.AllowDeadmansApparel);
        }

        public bool ShouldCheckWornClothes(ThingDef thingDef)
        {
            return _extendedBillData.CountWornApparel && thingDef.IsApparel;
        }

        public bool ShouldCheckEquippedWeapons(ThingDef thingDef)
        {
            return _extendedBillData.CountEquippedWeapons && thingDef.IsWeapon;
        }

        public bool DoesThingOnMapMatchFilter(ThingFilter ingredientFilter, Thing thing)
        {
            return IsThingInAppropriateStockpile(thing) && DoesThingMatchFilter(ingredientFilter, thing);
        }

        public bool DoesThingMatchFilter(ThingFilter ingredientFilter, Thing thing)
        {
            if (_extendedBillData.UseInputFilter
                && thing.Stuff != null
                && ingredientFilter != null)
            {
                if (!ingredientFilter.Allows(thing.Stuff))
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