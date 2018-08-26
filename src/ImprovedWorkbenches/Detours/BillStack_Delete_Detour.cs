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
            if (bill is Bill_Production billProduction)
            {
                Main.Instance.OnBillDeleted(billProduction);
            }

            return true;
        }
    }
}