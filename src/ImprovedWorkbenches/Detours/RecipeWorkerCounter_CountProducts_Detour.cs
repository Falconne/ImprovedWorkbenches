using System.Collections.Generic;
using System.Linq;
using System.Net;
using Harmony;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(RecipeWorkerCounter), "CountProducts")]
    public static class RecipeWorkerCounter_CountProducts_Detour
    {
        public static void Postfix(ref RecipeWorkerCounter __instance, ref int __result, ref Bill_Production bill)
        {
            var extendedBillData = Main.Instance.GetExtendedBillDataStorage().GetExtendedDataFor(bill);
            if (extendedBillData == null)
                return;

            if (extendedBillData.ProductAdditionalFilter != null)
            {
                __result += CountAdditionalProducts(__instance, bill, extendedBillData);
            }

            if (!ExtendedBillDataStorage.CanOutputBeFiltered(bill))
                return;

            var productThingDef = bill.recipe.products.First().thingDef;

            // Count resource items not in stockpiles
            if (Main.Instance.ShouldCountOutsideStockpiles()
                && productThingDef.CountAsResource
                && !bill.includeEquipped
                && (bill.includeTainted || !productThingDef.IsApparel || !productThingDef.apparel.careIfWornByCorpse)
                && bill.includeFromZone == null
                && bill.hpRange.min == 0f
                && bill.hpRange.max == 1f
                && bill.qualityRange.min == QualityCategory.Awful
                && bill.qualityRange.max == QualityCategory.Legendary
                && !bill.limitToAllowedStuff)
            {
                __result += GetMatchingItemCountOutsideStockpiles(bill, productThingDef);
            }

            if (!bill.includeEquipped)
                return;

            if (extendedBillData.CountAway)
                __result += CountAway(bill.Map, __instance, bill, productThingDef);
        }

        private static int GetMatchingItemCountOutsideStockpiles(Bill bill, ThingDef productThingDef)
        {
            var result = 0;

            var thingsOnMap = bill.Map.listerThings.ThingsOfDef(productThingDef);
            foreach (var thing in thingsOnMap)
            {
                if (thing.Position == IntVec3.Invalid || thing.IsNotFresh())
                {
                    continue;
                }

                if (thing.GetSlotGroup() != null)
                    continue;

                result += thing.stackCount;
            }

            return result;
        }

        private static int CountAway(Map billMap, RecipeWorkerCounter counter, Bill_Production bill, ThingDef productThingDef)
        {
            int count = 0;
            // Look for matching items in colonists and animals away from base
            foreach (var pawn in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction)
            {
                if (pawn.GetOriginMap() == billMap)
                    // OriginMap is only set on our pawns who are away from base
                    count += CountPawnThings(pawn, counter, bill, productThingDef);
            }
            return count;
        }

        private static int CountPawnThings(Pawn pawn, RecipeWorkerCounter counter, Bill_Production bill, ThingDef productThingDef, bool onlyCarry = false)
        {
            int count = 0;
            if (pawn.apparel != null && !onlyCarry)
                count += CountMatchingThingsIn(pawn.apparel.WornApparel.Cast<Thing>(), counter, bill, productThingDef);

            if (pawn.equipment != null && !onlyCarry)
                count += CountMatchingThingsIn(pawn.equipment.AllEquipmentListForReading.Cast<Thing>(), counter, bill, productThingDef);

            if (pawn.inventory != null && !onlyCarry)
                count += CountMatchingThingsIn(pawn.inventory.innerContainer, counter, bill, productThingDef);

            if (pawn.carryTracker != null) //Bill product immediately carried to stockpile should be counted
                count += CountMatchingThingsIn(pawn.carryTracker.innerContainer, counter, bill, productThingDef);

            return count;
        }

        // Helper function to count matching items in inventory lists
        private static int CountMatchingThingsIn(IEnumerable<Thing> things, RecipeWorkerCounter counterClass,
            Bill_Production bill, ThingDef productThingDef)
        {
            var count = 0;
            foreach (var thing in things)
            {
                Thing item = thing.GetInnerIfMinified();
                if (counterClass.CountValidThing(thing, bill, productThingDef))
                {
                    count += item.stackCount;
                }
            }

            return count;
        }

        // Count other things on map for ProductAdditionalFilter
        // This is sadly most of CountProducts re-written with a for loop for the additional defs
        public static int CountAdditionalProducts(RecipeWorkerCounter counter, Bill_Production bill, ExtendedBillData extendedBillData)
        {
            ThingFilter filter = extendedBillData.ProductAdditionalFilter;
            bool countAway = extendedBillData.CountAway;

            Map map = bill.Map;
            ThingDef defaultProductDef = counter.recipe.products[0].thingDef;
            int count = 0;
            foreach (ThingDef def in filter.AllowedThingDefs)
            {
                //Obviously skip the default product, it was already counted
                if (def == defaultProductDef) continue;

                //Same as CountProducts but now with other products
                if (def.CountAsResource && !bill.includeEquipped && (bill.includeTainted || !def.IsApparel || !def.apparel.careIfWornByCorpse) && bill.includeFromZone == null && bill.hpRange.min == 0f && bill.hpRange.max == 1f && bill.qualityRange.min == QualityCategory.Awful && bill.qualityRange.max == QualityCategory.Legendary && !bill.limitToAllowedStuff)
                {
                    count += map.resourceCounter.GetCount(def);
                    foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
                    {
                        count += CountPawnThings(pawn, counter, bill, def, true);
                    }
                }
                else if (bill.includeFromZone == null)
                {
                    count += counter.CountValidThings(map.listerThings.ThingsOfDef(def), bill, def);
                    if (def.Minifiable)
                    {
                        List<Thing> list = map.listerThings.ThingsInGroup(ThingRequestGroup.MinifiedThing);
                        for (int i = 0; i < list.Count; i++)
                        {
                            MinifiedThing minifiedThing = (MinifiedThing)list[i];
                            if (counter.CountValidThing(minifiedThing.InnerThing, bill, def))
                            {
                                count += minifiedThing.stackCount * minifiedThing.InnerThing.stackCount;
                            }
                        }
                    }

                    if (!bill.includeEquipped)
                    {
                        //Still count Carried Things
                        foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
                        {
                            count += CountPawnThings(pawn, counter, bill, def, true);
                        }
                    }
                }
                else
                {
                    foreach (Thing current in bill.includeFromZone.AllContainedThings)
                    {
                        Thing innerIfMinified = current.GetInnerIfMinified();
                        if (counter.CountValidThing(innerIfMinified, bill, def))
                        {
                            count += innerIfMinified.stackCount;
                        }
                    }
                }

                if (bill.includeEquipped)
                {
                    foreach (Pawn pawn in map.mapPawns.FreeColonistsSpawned)
                    {
                        count += CountPawnThings(pawn, counter, bill, def);
                    }
                }
                if (countAway)
                {
                    count += CountAway(map, counter, bill, def);
                }
            }
            return count;
        }
    }
}