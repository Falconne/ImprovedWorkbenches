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
            if (isThingAResource && !extendedBillData.UsesCountingStockpile() && !extendedBillData.CountInventory)
                return true;

            var statFilterWrapper = new StatFilterWrapper(extendedBillData);

            if (!statFilterWrapper.IsAnyFilteringNeeded(productThingDef))
                return true;


            __result = 0;
            if (productThingDef.Minifiable)
            {
                var minifiedThings = bill.Map.listerThings.ThingsInGroup(ThingRequestGroup.MinifiedThing);
                foreach (var thing in minifiedThings)
                {
                    var minifiedThing = (MinifiedThing)thing;
                    var innerThing = minifiedThing.InnerThing;
                    if (innerThing.def == productThingDef &&
                        statFilterWrapper.DoesThingOnMapMatchFilter(bill.ingredientFilter, innerThing) &&
                        statFilterWrapper.DoesThingOnMapMatchFilter(bill.ingredientFilter, minifiedThing))
                    {
                        __result++;
                    }
                }
            }

            SpecialThingFilterWorker_NonDeadmansApparel nonDeadmansApparelFilter = null;
            if (statFilterWrapper.ShouldCheckDeadman(productThingDef))
            {
                // We want to filter out corpse worn apparel
                nonDeadmansApparelFilter = new SpecialThingFilterWorker_NonDeadmansApparel();
                if (!nonDeadmansApparelFilter.CanEverMatch(productThingDef))
                    // Not apparel, don't bother checking
                    nonDeadmansApparelFilter = null;
            }

            if (statFilterWrapper.ShouldCheckMap(productThingDef))
            {
                var thingList = bill.Map.listerThings.ThingsOfDef(productThingDef).ToList();
                foreach (var thing in thingList)
                {
                    if (!statFilterWrapper.DoesThingOnMapMatchFilter(bill.ingredientFilter, thing))
                        continue;

                    if (nonDeadmansApparelFilter != null && !nonDeadmansApparelFilter.Matches(thing))
                        continue;

                    __result += thing.stackCount;
                }
            }

            //Who could have this Thing
            IEnumerable<Pawn> pawns = bill.Map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer).Where(
                p => p.IsFreeColonist || !p.IsColonist);    //Filter out prisoners, include animals (for inventory)

            //Gather the Things
            List<Thing> pawnThings = new List<Thing>();
            foreach (var pawn in pawns)
            {
                if (statFilterWrapper.ShouldCheckEquippedWeapons(productThingDef) && pawn.equipment != null)
                    pawnThings.AddRange(pawn.equipment.AllEquipmentListForReading.Cast<Thing>());
                if (statFilterWrapper.ShouldCheckWornClothes(productThingDef) && pawn.apparel != null)
                    pawnThings.AddRange(pawn.apparel.WornApparel.Cast<Thing>());
                if (statFilterWrapper.ShouldCheckInventory(productThingDef))
                {
                    if (pawn.inventory != null)
                        pawnThings.AddRange(pawn.inventory.innerContainer);
                    if (pawn.carryTracker != null)
                        pawnThings.AddRange(pawn.carryTracker.innerContainer);
                }
            }

            //Count the Things
            foreach (Thing i in pawnThings)
            {
                Thing item = MinifyUtility.GetInnerIfMinified(i);
                if ((item.def == productThingDef && statFilterWrapper.DoesThingMatchFilter(bill.ingredientFilter, item)) &&
                    (nonDeadmansApparelFilter?.Matches(item) ?? true))
                    __result += item.stackCount;
            }

            return false;
        }
    }
}