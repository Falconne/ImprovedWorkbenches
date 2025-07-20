using System.Collections.Generic;
using System.Linq;
using System.Net;
using HarmonyLib;
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

            ThingDef defaultProduct = bill.recipe?.products?.First().thingDef;

            if (extendedBillData.ProductAdditionalFilter != null)
            {
                foreach (ThingDef def in extendedBillData.ProductAdditionalFilter.AllowedThingDefs)
                {
                    __result += CountProducts(extendedBillData, def, bill, __instance, def == defaultProduct);
                }
            }

            if (!ExtendedBillDataStorage.CanOutputBeFiltered(bill))
                return;

            __result += CountProducts(extendedBillData, defaultProduct, bill, __instance, true);
        }

        public static int CountProducts(ExtendedBillData extendedBillData, ThingDef productThingDef, Bill_Production bill, RecipeWorkerCounter recipeWorkerCounter, bool defaultProduct)
        {
            int count = 0;

            // Count equipped items of additional products
            if (bill.includeEquipped && !defaultProduct)
            {
                foreach (Pawn pawn in AllPawnsToCount(bill.Map.mapPawns))
                {
                    count += CountPawnApparel(pawn, recipeWorkerCounter, bill, productThingDef);
                    count += CountPawnEquipment(pawn, recipeWorkerCounter, bill, productThingDef);
                }
            }

            // Count carried by non-humans (for default and additional products)
            if (bill.includeEquipped && Main.Instance.ShouldCountCarriedByNonHumans())
            {
                foreach (Pawn pawn in AllNonHumanPawnsToCount(bill.Map.mapPawns))
                {
                    count += CountPawnApparel(pawn, recipeWorkerCounter, bill, productThingDef);
                    count += CountPawnEquipment(pawn, recipeWorkerCounter, bill, productThingDef);
                }
            }

            // Count away products
            if (extendedBillData.CountAway)
            {
                IEnumerable<Pawn> pawnsAway = PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_OfPlayerFaction;
                foreach (var pawn in pawnsAway)
                {
                    // OriginMap is only set on our pawns who are away from base
                    if (pawn.GetOriginMap() != bill.Map)
                        continue;

                    count += CountPawnInventory(pawn, recipeWorkerCounter, bill, productThingDef);
                    count += CountPawnCarryTracker(pawn, recipeWorkerCounter, bill, productThingDef);

                    if (!bill.includeEquipped)
                        continue;

                    count += CountPawnApparel(pawn, recipeWorkerCounter, bill, productThingDef);
                    count += CountPawnEquipment(pawn, recipeWorkerCounter, bill, productThingDef);
                }
            }

            // Count only specific slot group if "Look in Stockpile ..." is set.
            if (bill.GetIncludeSlotGroup() != null)
            {
                // return because default product is already counted by RimWorld
                if (defaultProduct)
                    return count;

                count += CountMatchingThingsIn(bill.GetIncludeSlotGroup().HeldThings, recipeWorkerCounter, bill, productThingDef);
                return count;
            }

            // Count Inventory - Compatibility with PickupandHaul (Ignore default product when includeEquipped is true)
            if (!defaultProduct || !bill.includeEquipped)
            {
                foreach (Pawn pawn in AllPawnsToCount(bill.Map.mapPawns))
                {
                    count += CountPawnInventory(pawn, recipeWorkerCounter, bill, productThingDef);
                    count += CountPawnCarryTracker(pawn, recipeWorkerCounter, bill, productThingDef);
                }
            }

            // Check if we can count easily with resource counter 
            if (productThingDef.CountAsResource
                && !bill.includeEquipped
                && (bill.includeTainted || !productThingDef.IsApparel || !productThingDef.apparel.careIfWornByCorpse)
                && bill.hpRange.min == 0f
                && bill.hpRange.max == 1f
                && bill.qualityRange.min == QualityCategory.Awful
                && bill.qualityRange.max == QualityCategory.Legendary
                && !bill.limitToAllowedStuff)
            {
                // resourceCounter only counts products in stockpiles
                if (!defaultProduct)
                    count += bill.Map.resourceCounter.GetCount(productThingDef);

                if (Main.Instance.ShouldCountOutsideStockpiles())
                {
                    IEnumerable<Thing> thingsOnMap = bill.Map.listerThings.ThingsOfDef(productThingDef);
                    foreach (Thing thing in thingsOnMap)
                    {
                        if (thing.Position == IntVec3.Invalid || thing.IsNotFresh())
                            continue;

                        if (thing.GetSlotGroup() != null)
                            continue;

                        if (thing.Fogged())
                            continue;

                        count += thing.stackCount;
                    }
                }

                return count;
            }

            // Skip default product if it is a resource because it is already counted by RimWorld
            if (defaultProduct && !productThingDef.CountAsResource)
                return count;

            // Count things on map, not as fast as resourceCounter but more accurate because of "CountValidThing" function checks every thing for validation
            IEnumerable<Thing> things = bill.Map.listerThings.ThingsOfDef(productThingDef);
            foreach (Thing thing in things)
            {
                // Skip things that are not in a Stockpile if "Count outside stockpiles" is not enabled
                if (thing.GetSlotGroup() == null && !Main.Instance.ShouldCountOutsideStockpiles())
                    continue;
                // Skip things that are in a Stockpile if it is the default product (Already counted by RimWorld)
                if (thing.GetSlotGroup() != null && defaultProduct)
                    continue;
                // Skip things that are not valid for the recipe
                if (!recipeWorkerCounter.CountValidThing(thing, bill, productThingDef))
                    continue;

                count += thing.stackCount;
            }

            if (!productThingDef.Minifiable)
                return count;

            // Check all minified things on the map if the unminified thing is a valid product
            IEnumerable<MinifiedThing> minifiedThings = bill.Map.listerThings.ThingsInGroup(ThingRequestGroup.MinifiedThing).Cast<MinifiedThing>();
            foreach (MinifiedThing minifiedThing in minifiedThings)
            {
                if (recipeWorkerCounter.CountValidThing(minifiedThing.InnerThing, bill, productThingDef))
                    count += minifiedThing.stackCount * minifiedThing.InnerThing.stackCount;
            }

            return count;
        }

        private static IEnumerable<Pawn> AllPawnsToCount(MapPawns mapPawns)
        {
            if (!Main.Instance.ShouldCountCarriedByNonHumans())
                return mapPawns.FreeColonistsSpawned;
            return mapPawns.FreeColonistsSpawned.Concat(AllNonHumanPawnsToCount(mapPawns));
        }

        private static IEnumerable<Pawn> AllNonHumanPawnsToCount(MapPawns mapPawns)
        {
            return mapPawns.SpawnedColonyAnimals
                .Concat(mapPawns.SpawnedColonyMechs)
                .Concat(mapPawns.SpawnedColonySubhumansPlayerControlled);
        }

        private static int CountPawnApparel(Pawn pawn, RecipeWorkerCounter counter, Bill_Production bill, ThingDef productThingDef)
        {
            if (pawn.apparel != null)
                return CountMatchingThingsIn(pawn.apparel.WornApparel.Cast<Thing>(), counter, bill, productThingDef);
            return 0;
        }

        private static int CountPawnEquipment(Pawn pawn, RecipeWorkerCounter counter, Bill_Production bill, ThingDef productThingDef)
        {
            if (pawn.equipment != null)
                return CountMatchingThingsIn(pawn.equipment.AllEquipmentListForReading.Cast<Thing>(), counter, bill, productThingDef);
            return 0;
        }

        private static int CountPawnInventory(Pawn pawn, RecipeWorkerCounter counter, Bill_Production bill, ThingDef productThingDef)
        {
            if (pawn.inventory != null)
                return CountMatchingThingsIn(pawn.inventory.innerContainer, counter, bill, productThingDef);
            return 0;
        }

        private static int CountPawnCarryTracker(Pawn pawn, RecipeWorkerCounter counter, Bill_Production bill, ThingDef productThingDef)
        {
            if (pawn.carryTracker != null)
                return CountMatchingThingsIn(pawn.carryTracker.innerContainer, counter, bill, productThingDef);
            return 0;
        }

        // Helper function to count matching items in inventory lists
        private static int CountMatchingThingsIn(IEnumerable<Thing> things, RecipeWorkerCounter counterClass,
            Bill_Production bill, ThingDef productThingDef)
        {
            var count = 0;
            foreach (var thing in things)
            {
                Thing item = thing.GetInnerIfMinified();
                if (counterClass.CountValidThing(item, bill, productThingDef))
                {
                    count += item.stackCount;
                }
            }

            return count;
        }
    }
}