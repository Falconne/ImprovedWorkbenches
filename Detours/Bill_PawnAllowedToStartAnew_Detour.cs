using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(Bill), "PawnAllowedToStartAnew")]
    public static class Bill_PawnAllowedToStartAnew_Detour
    {
        [HarmonyPostfix]
        public static void Postfix(Bill __instance, ref bool __result, Pawn p)
        {
            if (!__result)
                return;

            var billProduction = __instance as Bill_Production;
            if (billProduction == null)
                return;

            var extendedBillData = Main.Instance.GetExtendedBillDataStorage().GetExtendedDataFor(billProduction);

            var assignedWorker = extendedBillData?.Worker;
            if (assignedWorker == null)
                return;

            if (assignedWorker != p)
                __result = false;
        }
    }
}
