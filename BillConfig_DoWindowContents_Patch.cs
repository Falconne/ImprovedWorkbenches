using System.Linq;
using Harmony;
using RimWorld;
using UnityEngine;
using Verse;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(Dialog_BillConfig), "DoWindowContents")]
    public static class BillConfig_DoWindowContents_Patch
    {
        [HarmonyPostfix]
        public static void DrawHelloButton(Dialog_BillConfig __instance, Rect inRect)
        {
            var billRaw = (Bill_Production)AccessTools.Field(typeof(Dialog_BillConfig), "bill").GetValue(__instance);
            if (billRaw.repeatMode != BillRepeatModeDefOf.TargetCount)
                return;

            if (!(billRaw is IBillWithThingFilter))
                return;

            var bill = billRaw as IBillWithThingFilter;

            var rect = new Rect(0, inRect.height - 200f, 160f, 40f);
            Widgets.Label(rect, "Output filter:");
            var row = inRect.height - 150f;
            DrawHitPointsFilterConfig(ref row, rect.width, bill.GetOutputFilter());
        }

        private static void DrawHitPointsFilterConfig(ref float y, float width, ThingFilter filter)
        {
            Rect rect = new Rect(0f, y, width, 26f);
            FloatRange allowedHitPointsPercents = filter.AllowedHitPointsPercents;
            Widgets.FloatRange(rect, 2, ref allowedHitPointsPercents, 0f, 1f, "HitPoints", ToStringStyle.PercentZero);
            filter.AllowedHitPointsPercents = allowedHitPointsPercents;
            y += 26f;
            y += 5f;
        }
    }
}