using HarmonyLib;
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
                // Clean up WorktableRestrictionData from WorldComponent
                var worldComp = Find.World.GetComponent<WorktableRestrictionDataStorage>();
                worldComp?.RemoveWorktableRestrictionData(workTable.thingIDNumber);

                // Clean up bill data 
                if (workTable.BillStack?.Bills != null)
                {
                    foreach (var bill in workTable.BillStack.Bills)
                    {
                        if (bill is Bill_Production billProduction)
                        {
                            Main.Instance.OnBillDeleted(billProduction);
                        }
                    }
                }
            }

            return true;
        }
    }
}