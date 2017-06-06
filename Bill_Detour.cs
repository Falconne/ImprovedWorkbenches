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
    public static class Bill_Detour
    {
        [HarmonyPostfix]
        public static void Postfix(Bill __instance, ref bool __result, Pawn p)
        {
            if (!__result)
                return;

            var bill = __instance as IBillWithWorkerFilter;
            if (bill == null)
                return;

            var assignedWorker = bill.GetWorker();
            if (assignedWorker == null)
                return;

            if (assignedWorker != p)
                __result = false;
        }
    }
}
