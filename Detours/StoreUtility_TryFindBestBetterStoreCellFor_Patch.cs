using Harmony;
using RimWorld;
using UnityEngine;
using Verse;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(StoreUtility), "TryFindBestBetterStoreCellFor")]
    public class StoreUtility_TryFindBestBetterStoreCellFor_Patch
    {
        static bool Prefix(ref bool __result, Thing t, Pawn carrier, Map map, Faction faction, out IntVec3 foundCell)
        {
            foundCell = IntVec3.Invalid;
            if (carrier == null || faction == null || faction != Faction.OfPlayer || t.SpawnedOrAnyParentSpawned)
                return true;

            var bill = carrier.CurJob.bill as Bill_Production;
            if (bill == null)
                return true;

            if (bill.GetStoreMode() != BillStoreModeDefOf.BestStockpile)
                return true;

            var extendedBillData = Main.Instance.GetExtendedBillDataStorage().GetExtendedDataFor(bill);
            if (extendedBillData == null)
                return true;

            if (!extendedBillData.UsesTakeToStockpile())
                return true;

            var stockPile = extendedBillData.GetTakeToStockpile();
            if (!stockPile.settings.AllowedToAccept(t))
                return true;

            var cellsList = stockPile.GetSlotGroup().CellsList;
            var cellCount = cellsList.Count;
            var searchAccuracy = Mathf.FloorToInt(cellCount * 0.012f);
            var startingLocation = carrier.PositionHeld;
            float bestDistanceFound = int.MaxValue;
            var foundGoodCell = false;

            for (var j = 0; j < cellCount; j++)
            {
                IntVec3 possibleDestination = cellsList[j];
                float distanceToDestination = (startingLocation - possibleDestination).LengthHorizontalSquared;

                if (!(distanceToDestination <= bestDistanceFound))
                    continue;

                if (!StoreUtility.IsGoodStoreCell(possibleDestination, map, t, carrier, faction))
                    continue;

                foundGoodCell = true;
                foundCell = possibleDestination;
                if (j >= searchAccuracy)
                {
                    break;
                }
                bestDistanceFound = distanceToDestination;
            }

            __result = foundGoodCell;
            return !foundGoodCell;
        }
    }
}