using HarmonyLib;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches.Detours
{
    [HarmonyPatch(typeof(BillUtility), "MakeNewBill")]
    public class BillUtility_MakeNewBill_Detour
    {
        public static void Postfix(ref Bill __result)
        {
            if (!(__result is Bill_Production billProduction))
                return;

            if (Main.Instance.ShouldDropOnFloorByDefault())
                billProduction?.SetStoreMode(BillStoreModeDefOf.DropOnFloor);

            if (!(Find.Selector.SingleSelectedThing is Building_WorkTable workTable))
                return;

            WorktableRestrictionData worktableRestrictionData = Find.World.GetComponent<WorktableRestrictionDataStorage>()?.GetWorktableRestrictionData(workTable.thingIDNumber);
            if (worktableRestrictionData != null && worktableRestrictionData.isRestricted)
                worktableRestrictionData.SetWorktableRestrictionToBill(billProduction);

        }
    }
}