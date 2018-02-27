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


            Map map = bill.Map;
            __result = 0;
            if (productThingDef.Minifiable)
            {
                var minifiedThings = map.listerThings.ThingsInGroup(ThingRequestGroup.MinifiedThing);
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
                var thingList = map.listerThings.ThingsOfDef(productThingDef).ToList();
                foreach (var thing in thingList)
                {
                    if (!statFilterWrapper.DoesThingOnMapMatchFilter(bill.ingredientFilter, thing))
                        continue;

                    if (nonDeadmansApparelFilter != null && !nonDeadmansApparelFilter.Matches(thing))
                        continue;

                    __result += thing.stackCount;
                }
            }

            if (statFilterWrapper.ShouldCheckInventory(productThingDef))
            {
                //Who could have this Thing
                List<Pawn> pawns = new List<Pawn>();

                //Only this map, or a thorough global search
                if (!statFilterWrapper.ShouldCheckAway(productThingDef))
                {
                    //Spawned only
                    pawns.AddRange(map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer)
                        .Where(p => p.IsFreeColonist || !p.IsColonist));    //Filter out prisoners, include animals (for inventory)
                }
                else
                {
                    //Include unspawned (transport pods, cryptosleep, etc.)
                    pawns.AddRange(map.mapPawns.PawnsInFaction(Faction.OfPlayer)
                        .Where(p => p.IsFreeColonist || !p.IsColonist));

                    // and caravans
                    pawns.AddRange(Find.WorldPawns.AllPawnsAlive.Where(p => p.GetOriginMap() == map));

                    // and at other maps (but who originated here)
                    foreach (Map otherMap in Find.Maps.Where(m => m != map))
                    {
                        pawns.AddRange(otherMap.mapPawns.PawnsInFaction(Faction.OfPlayer)
                            .Where(p => (p.IsFreeColonist || !p.IsColonist) && p.GetOriginMap() == map));
                    }
                }

                List<Thing> pawnThings = new List<Thing>();
                //Gather the Things
                foreach (var pawn in pawns)
                {
                    if (pawn.apparel != null)
                        pawnThings.AddRange(pawn.apparel.WornApparel.Cast<Thing>());
                    if (pawn.equipment != null)
                        pawnThings.AddRange(pawn.equipment.AllEquipmentListForReading.Cast<Thing>());
                    if (pawn.inventory != null)
                        pawnThings.AddRange(pawn.inventory.innerContainer);
                    if (pawn.carryTracker != null)
                        pawnThings.AddRange(pawn.carryTracker.innerContainer);
                }

                //Count the Things
                foreach (Thing i in pawnThings)
                {
                    Thing item = MinifyUtility.GetInnerIfMinified(i);
                    if ((item.def == productThingDef && statFilterWrapper.DoesThingMatchFilter(bill.ingredientFilter, item)) &&
                        (nonDeadmansApparelFilter?.Matches(item) ?? true))
                        __result += item.stackCount;
                }
            }

            return false;
        }
    }
}