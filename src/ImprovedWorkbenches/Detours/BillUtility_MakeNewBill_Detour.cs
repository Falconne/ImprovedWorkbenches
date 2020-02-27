using HarmonyLib;
using RimWorld;

namespace ImprovedWorkbenches.Detours
{
    [HarmonyPatch(typeof(BillUtility), "MakeNewBill")]
    public class BillUtility_MakeNewBill_Detour
    {
        public static void Postfix(ref Bill __result)
        {
            if (!Main.Instance.ShouldDropOnFloorByDefault())
                return;

            var billProduction = __result as Bill_Production;

            billProduction?.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        }
    }
}