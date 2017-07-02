using Harmony;
using RimWorld;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(Bill_Production), "get_RepeatInfoText")]
    public class Bill_Production_RepeatInfoText_Detour
    {
        [HarmonyPrefix]
        static bool Prefix(Bill_Production __instance, ref string __result)
        {
            if (__instance.repeatMode != BillRepeatModeDefOf.TargetCount ||
                !__instance.pauseWhenSatisfied)
            {
                return true;
            }

            var currentCount = __instance.recipe.WorkerCounter.CountProducts(__instance);

            __result = $"({__instance.unpauseWhenYouHave}) {currentCount}/{__instance.targetCount}";

            return false;
        }
    }
}