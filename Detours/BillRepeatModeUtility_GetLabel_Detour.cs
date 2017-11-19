using Harmony;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(BillRepeatModeUtility), "GetLabel")]
    public class BillRepeatModeUtility_GetLabel_Detour
    {
        public static bool Prefix(ref BillRepeatModeDef brm, ref string __result)
        {
            if (brm != BillRepeatModeDefOf.TargetCount)
                return true;
            __result = "IW.DoUntilXText".Translate();

            return false;
        }

    }
}