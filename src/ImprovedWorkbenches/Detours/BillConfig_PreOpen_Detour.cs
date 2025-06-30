using HarmonyLib;
using RimWorld;
using Verse;
using System.Reflection;


namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(Dialog_BillConfig), "PreOpen")]
    public static class Dialog_BillConfig_PreOpen_Detour
    {
        private static readonly FieldInfo BillGetter = typeof(Dialog_BillConfig).GetField("bill",
            BindingFlags.NonPublic | BindingFlags.Instance);

        [HarmonyPostfix]
        public static void Postfix(Dialog_BillConfig __instance)
        {

            Bill_Production bill = (Bill_Production)BillGetter.GetValue(__instance);
            if (bill == null)
                return;

            Building_WorkTable workTable = Find.Selector.SingleSelectedThing as Building_WorkTable;
            WorktableRestrictionData workbenchRestrictionData = Find.World.GetComponent<WorktableRestrictionDataStorage>()?.GetWorktableRestrictionData(workTable.thingIDNumber);

            if (workbenchRestrictionData == null || !workbenchRestrictionData.isRestricted)
                return;

            if (workbenchRestrictionData.restrictionPawn != null)
                bill.SetPawnRestriction(workbenchRestrictionData.restrictionPawn);
            else if (workbenchRestrictionData.restrictionSlavesOnly)
                bill.SetAnySlaveRestriction();
            else if (workbenchRestrictionData.restrictionMechsOnly)
                bill.SetAnyMechRestriction();
            else if (workbenchRestrictionData.restrictionNonMechsOnly)
                bill.SetAnyNonMechRestriction();
            else
                bill.SetAnyPawnRestriction();

            bill.allowedSkillRange = workbenchRestrictionData.restrictionAllowedSkillRange;
        }
    }
}