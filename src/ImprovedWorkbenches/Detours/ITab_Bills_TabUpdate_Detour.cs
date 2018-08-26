using System.Reflection;
using Harmony;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(ITab_Bills), "TabUpdate")]
    public static class ITab_Bills_TabUpdate_Detour
    {
        private static readonly FieldInfo MouseOverBillGetter = typeof(ITab_Bills).GetField("mouseoverBill",
            BindingFlags.NonPublic | BindingFlags.Instance);

        [HarmonyPrefix]
        public static bool Prefix(ref ITab_Bills __instance)
        {
            var bill = MouseOverBillGetter.GetValue(__instance) as Bill_Production;
            if (bill == null)
                return true;

            Main.Instance.GetExtendedBillDataStorage().MirrorBillToLinkedBills(bill);
            return true;
        }
    }
}