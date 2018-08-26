using Harmony;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(ZoneManager), "DeregisterZone")]
    public class ZoneManager_DeregisterZone_Patch
    {
        static bool Prefix(Zone oldZone)
        {
            var stockpile = oldZone as Zone_Stockpile;
            if (stockpile == null)
                return true;

            Main.Instance.GetExtendedBillDataStorage().OnStockpileDeteled(stockpile);
            return true;
        }
    }
}