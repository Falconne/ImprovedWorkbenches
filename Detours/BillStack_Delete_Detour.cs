using Harmony;
using RimWorld;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(BillStack), "Delete")]
    public static class BillStack_Delete_Detour
    {
        [HarmonyPrefix]
        public static bool Prefix(Bill bill)
        {
            var billProduction = bill as Bill_Production;
            if (billProduction == null)
                return true;
            Main.Instance.GetExtendedBillDataStorage().DeleteExtendedDataFor(billProduction);
            return true;
        }
    }
}