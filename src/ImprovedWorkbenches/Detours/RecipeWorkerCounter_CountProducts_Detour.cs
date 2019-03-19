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
        [HarmonyPrefix]
        public static void Postfix(ref RecipeWorkerCounter __instance, ref int __result, ref Bill_Production bill)
        {
            if (!bill.includeEquipped)
                return;

            if (!ExtendedBillDataStorage.CanOutputBeFiltered(bill))
                return;

            var billMap = bill.Map;

            var productThingDef = bill.recipe.products.First().thingDef;
            // Fix for vanilla not counting items being hauled by colonists or animals
            foreach (var pawn in billMap.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer))
            {
                // Ignore prisoners, include animals
                if (!(pawn.IsFreeColonist || !pawn.IsColonist))
                    continue;

                if (pawn.carryTracker != null)
                    __result += CountMatchingThingsIn(
                        pawn.carryTracker.innerContainer, __instance, bill,productThingDef);
            }

            var extendedBillData = Main.Instance.GetExtendedBillDataStorage().GetExtendedDataFor(bill);
            if (extendedBillData == null || !extendedBillData.CountAway)
                return;


            // Look for matching items in colonists and animals away from base
            foreach (var pawn in PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_OfPlayerFaction)
            {
                if (pawn.GetOriginMap() != billMap)
                    // OriginMap is only set on our pawns who are away from base
                    continue;

                if (pawn.apparel != null)
                    __result += CountMatchingThingsIn(pawn.apparel.WornApparel.Cast<Thing>(), __instance, bill, productThingDef);

                if (pawn.equipment != null)
                    __result += CountMatchingThingsIn(pawn.equipment.AllEquipmentListForReading.Cast<Thing>(), __instance, bill, productThingDef);

                if (pawn.inventory != null)
                    __result += CountMatchingThingsIn(pawn.inventory.innerContainer, __instance, bill, productThingDef);

                if (pawn.carryTracker != null)
                    __result += CountMatchingThingsIn(pawn.carryTracker.innerContainer, __instance, bill, productThingDef);
            }
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
   }
}