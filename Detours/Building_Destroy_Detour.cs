using Harmony;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(Building), "Destroy")]
    public class Building_Destroy_Detour
    {
        public static bool Prefix(Building __instance)
        {
            if (__instance is Building_WorkTable workTable)
            {
                foreach (var bill in workTable.BillStack.Bills)
                {
                    if (bill is Bill_Production billProduction)
                    {
                        Main.Instance.OnBillDeleted(billProduction);
                    }
                }
            }

            return true;
        }
    }
}