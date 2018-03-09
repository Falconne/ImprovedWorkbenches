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

            var thingCountClass = bill.recipe.products.First();
            var productThingDef = thingCountClass.thingDef;

            var statFilterWrapper = new StatFilterWrapper(extendedBillData);

            if (!statFilterWrapper.IsAnyFilteringNeeded(productThingDef))
                return true;


            var billMap = bill.Map;
            var billIngredientFilter = bill.ingredientFilter;
            __result = 0;
            if (productThingDef.Minifiable)
            {
                // Minified items must be counted separately, to differentiate them from installed items.

                var minifiedThings = billMap.listerThings.ThingsInGroup(ThingRequestGroup.MinifiedThing);
                foreach (var thing in minifiedThings)
                {
                    var minifiedThing = (MinifiedThing)thing;
                    var innerThing = minifiedThing.InnerThing;
                    if (innerThing.def == productThingDef &&
                        statFilterWrapper.DoesThingOnMapMatchFilter(billIngredientFilter, innerThing) &&
                        statFilterWrapper.DoesThingOnMapMatchFilter(billIngredientFilter, minifiedThing))
                    {
                        __result++;
                    }
                }
            }

            SpecialThingFilterWorker_NonDeadmansApparel nonDeadmansApparelFilter =
                statFilterWrapper.TryGetDeadmanFilter(productThingDef);

            if (statFilterWrapper.ShouldCheckMap(productThingDef))
            {
                // Count items on the ground, in shelves, etc.

                var thingList = billMap.listerThings.ThingsOfDef(productThingDef).ToList();
                foreach (var thing in thingList)
                {
                    if (!statFilterWrapper.DoesThingOnMapMatchFilter(billIngredientFilter, thing))
                        continue;

                    if (nonDeadmansApparelFilter != null && !nonDeadmansApparelFilter.Matches(thing))
                        continue;

                    __result += thing.stackCount;
                }
            }
            else if (productThingDef.CountAsResource)
            {
                // Above clause will count "resource" type items in a specific stockpile, if
                // UsesCountingStockpile() is true. If it isn't, let the vanilla code count
                // resoucres in all stockpiles.
                __result += billMap.resourceCounter.GetCount(thingCountClass.thingDef);
            }

            if (!statFilterWrapper.ShouldCheckInventory(productThingDef))
                return false;

            // Find player pawns to check inventories of
            var playerFactionPawnsToCheck = new List<Pawn>();
            if (!statFilterWrapper.ShouldCheckAway(productThingDef))
            {
                // Only check bill map, spawned pawns only
                playerFactionPawnsToCheck.AddRange(billMap.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer)
                    //Filter out prisoners, but include animals (for inventory)
                    .Where(p => p.IsFreeColonist || !p.IsColonist));
            }
            else
            {
                // Given a colonist or animal from the player faction, check if its home map
                // is the bill's map.
                bool IsPlayerPawnFromBillMap(Pawn pawn)
                {
                    if (!pawn.IsFreeColonist && pawn.RaceProps.Humanlike)
                        return false;

                    // Assumption: pawns transferring between colonies will "settle"
                    // in the destination.

                    return pawn.Map == billMap || pawn.GetOriginMap() == billMap;
                }

                // Include all colonists and colony animals, unspawned (transport pods, cryptosleep, etc.)
                // in all maps.
                foreach (Map otherMap in Find.Maps)
                {
                    playerFactionPawnsToCheck.AddRange(
                        otherMap.mapPawns.PawnsInFaction(Faction.OfPlayer)
                            .Where(IsPlayerPawnFromBillMap));
                }

                // and caravans
                playerFactionPawnsToCheck.AddRange(Find.WorldPawns.AllPawnsAlive
                    // OriginMap is only set on pawns we care about
                    .Where(p => p.GetOriginMap() == billMap));
            }


            // Helper function to count matching items in inventory lists
            int CountMatchingThingsIn(IEnumerable<Thing> things)
            {
                var count = 0;
                foreach (Thing thing in things)
                {
                    Thing item = thing.GetInnerIfMinified();
                    if (item.def != productThingDef)
                        continue;

                    if (!statFilterWrapper.DoesThingMatchFilter(billIngredientFilter, item))
                        continue;

                    if (nonDeadmansApparelFilter != null && !nonDeadmansApparelFilter.Matches(item))
                        continue;

                    count += item.stackCount;
                }

                return count;
            }

            // Look for matching items in found colonist inventories
            foreach (var pawn in playerFactionPawnsToCheck)
            {
                if (pawn.apparel != null)
                    __result += CountMatchingThingsIn(pawn.apparel.WornApparel.Cast<Thing>());

                if (pawn.equipment != null)
                    __result += CountMatchingThingsIn(pawn.equipment.AllEquipmentListForReading.Cast<Thing>());

                if (pawn.inventory != null)
                    __result += CountMatchingThingsIn(pawn.inventory.innerContainer);

                if (pawn.carryTracker != null)
                    __result += CountMatchingThingsIn(pawn.carryTracker.innerContainer);
            }

            return false;
        }
    }
}