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
                || ShouldCheckInventory(thingDef)
                || ShouldCheckDeadman(thingDef)
                || thingDef.Minifiable;
        }

        public bool ShouldCheckInventory(ThingDef thingDef)
        {
            return _extendedBillData.CountInventory && GoesInInventory(thingDef);
        }

        public bool ShouldCheckAway(ThingDef thingDef)
        {
            return _extendedBillData.CountAway && GoesInInventory(thingDef);
        }

        public static bool GoesInInventory(ThingDef thingDef)
        {
            // Probably redundant, but I'm sure something out there doesn't match O_o
            return (thingDef.IsApparel || thingDef == ThingDefOf.Apparel_ShieldBelt ||
                thingDef.IsWeapon ||
                (thingDef.minifiedDef?.EverHaulable ?? thingDef.EverHaulable));
        }

        public bool ShouldCheckDeadman(ThingDef thingDef)
        {
            return !_extendedBillData.AllowDeadmansApparel && thingDef.IsApparel;
        }

        public bool ShouldCheckMap(ThingDef thingDef)
        {
            return (_extendedBillData.CountInstalled && thingDef.Minifiable) || !thingDef.Minifiable;
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