using Harmony;
using RimWorld;
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

            return false;
        }
    }
}