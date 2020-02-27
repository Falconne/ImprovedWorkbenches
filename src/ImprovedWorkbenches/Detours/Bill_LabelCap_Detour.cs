using HarmonyLib;
using RimWorld;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(Bill), "get_LabelCap")]
    public class Bill_LabelCap_Detour
    {
        [HarmonyPrefix]
        static bool Prefix(Bill __instance, ref string __result)
        {
            var bill = __instance as Bill_Production;
            if (bill == null)
                return true;

            var extendedBillData = Main.Instance.GetExtendedBillDataStorage().GetOrCreateExtendedDataFor(bill);

            if (string.IsNullOrEmpty(extendedBillData?.Name))
                return true;

            __result = extendedBillData.Name;

            return false;
        }

    }
}