using HarmonyLib;
using RimWorld;
using Verse;
using System.Reflection;


namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(Window), "PreClose")]
    public static class BillConfig_PreClose_Detour
    {

        private static readonly FieldInfo BillGetter = typeof(Dialog_BillConfig).GetField("bill",
            BindingFlags.NonPublic | BindingFlags.Instance);

        [HarmonyPostfix]
        public static void Postfix(Window __instance)
        {
            if (__instance.GetType() != typeof(Dialog_BillConfig)) return;

            if (!(BillGetter?.GetValue(__instance) is Bill_Production bill) || bill == null) return;

            if (!(Find.Selector?.SingleSelectedThing is Building_WorkTable workTable)) return;

            WorktableRestrictionData workbenchRestrictionData = Find.World.GetComponent<WorktableRestrictionDataStorage>()?.GetWorktableRestrictionData(workTable.thingIDNumber);
            if (workbenchRestrictionData == null || !workbenchRestrictionData.isRestricted) return;

            // Skip if the restriction has not changed
            if (workbenchRestrictionData.restrictionPawn == bill.PawnRestriction &&
                workbenchRestrictionData.restrictionSlavesOnly == bill.SlavesOnly &&
                workbenchRestrictionData.restrictionMechsOnly == bill.MechsOnly &&
                workbenchRestrictionData.restrictionNonMechsOnly == bill.NonMechsOnly &&
                workbenchRestrictionData.restrictionAllowedSkillRange == bill.allowedSkillRange)
            {
                return;
            }

            // Update the restriction data in case all bills get deleted
            workbenchRestrictionData.restrictionPawn = bill.PawnRestriction;
            workbenchRestrictionData.restrictionSlavesOnly = bill.SlavesOnly;
            workbenchRestrictionData.restrictionMechsOnly = bill.MechsOnly;
            workbenchRestrictionData.restrictionNonMechsOnly = bill.NonMechsOnly;
            workbenchRestrictionData.restrictionAllowedSkillRange = bill.allowedSkillRange;

            if (workTable.BillStack?.Bills == null) return;

            // Update all bills in the worktable to match the restriction data
            if (workbenchRestrictionData.restrictionPawn != null)
            {
                workTable.BillStack.Bills.ForEach(b =>
                {
                    b.SetPawnRestriction(workbenchRestrictionData.restrictionPawn);
                    b.allowedSkillRange = workbenchRestrictionData.restrictionAllowedSkillRange;
                });
            }
            else if (workbenchRestrictionData.restrictionSlavesOnly)
            {
                workTable.BillStack.Bills.ForEach(b =>
                {
                    b.SetAnySlaveRestriction();
                    b.allowedSkillRange = workbenchRestrictionData.restrictionAllowedSkillRange;
                });
            }
            else if (workbenchRestrictionData.restrictionMechsOnly)
            {
                workTable.BillStack.Bills.ForEach(b =>
                {
                    b.SetAnyMechRestriction();
                    b.allowedSkillRange = workbenchRestrictionData.restrictionAllowedSkillRange;
                });
            }
            else if (workbenchRestrictionData.restrictionNonMechsOnly)
            {
                workTable.BillStack.Bills.ForEach(b =>
                {
                    b.SetAnyNonMechRestriction();
                    b.allowedSkillRange = workbenchRestrictionData.restrictionAllowedSkillRange;
                });
            }
            else
            {
                workTable.BillStack.Bills.ForEach(b =>
                {
                    b.SetAnyPawnRestriction();
                    b.allowedSkillRange = workbenchRestrictionData.restrictionAllowedSkillRange;
                });
            }
        }
    }
}