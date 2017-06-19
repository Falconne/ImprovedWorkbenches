using System.Collections.Generic;
using Harmony;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(BillRepeatModeUtility), "MakeConfigFloatMenu")]
    public class BillRepeatModeUtility_MakeConfigFloatMenu_Detour
    {
        [HarmonyPrefix]
        static bool Prefix(Bill_Production bill)
        {
            if (!Main.Instance.GetExtendedBillDataStorage().IsLinkedBill(bill))
                return true;

            var list = new List<FloatMenuOption>();
            list.Add(new FloatMenuOption("Use Details to change linked bill mode", delegate {}));
            Find.WindowStack.Add(new FloatMenu(list));

            return false;
        }
    }
}