using System.Reflection;
using HarmonyLib;
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
            if (!(MouseOverBillGetter.GetValue(__instance) is Bill_Production bill))
                return true;

            if (BillUtility.Clipboard != null)
            {
                Main.Instance.BillCopyPasteHandler.DoCopy(bill);
                BillUtility.Clipboard = null;
            }

            Main.Instance.GetExtendedBillDataStorage().MirrorBillToLinkedBills(bill);
            return true;
        }
    }
}