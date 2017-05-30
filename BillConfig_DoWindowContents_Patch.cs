using System;
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
        public static void DrawFilters(Dialog_BillConfig __instance, Rect inRect)
        {
            var billRaw = (Bill_Production)AccessTools.Field(typeof(Dialog_BillConfig), "bill").GetValue(__instance);
            if (billRaw.repeatMode != BillRepeatModeDefOf.TargetCount)
                return;

            var bill = billRaw as IBillWithThingFilter;
            if (bill == null)
                return;

            var filter = bill.GetOutputFilter();

            const float columnWidth = 180f;
            const float gap = 26f;
            var rect = new Rect(0, inRect.height - 210f, columnWidth, 40f);
            Widgets.Label(rect, "Counted items filter:");
            var y = rect.yMin + Text.LineHeight - 1;

            var rect1 = new Rect(0f, y, columnWidth, gap);
            var allowedHitPointsPercents = filter.AllowedHitPointsPercents;
            Widgets.FloatRange(rect1, 10, ref allowedHitPointsPercents, 0f, 1f, 
                "HitPoints", ToStringStyle.PercentZero);
            filter.AllowedHitPointsPercents = allowedHitPointsPercents;

            if (!filter.allowedQualitiesConfigurable)
                return;

            y += 33;
            var rect2 = new Rect(0f, y, columnWidth, gap);
            var allowedQualityLevels = filter.AllowedQualityLevels;
            Widgets.QualityRange(rect2, 11, ref allowedQualityLevels);
            filter.AllowedQualityLevels = allowedQualityLevels;

            var nonDeadmansApparelFilter = new SpecialThingFilterWorker_NonDeadmansApparel();
            var thingDef = bill.GetRecipeDef().products.First().thingDef;
            if (!nonDeadmansApparelFilter.CanEverMatch(thingDef))
            {
                // Not apparel, so deadman check is not needed.
                return;
            }
            y += 35;
            var rect3 = new Rect(0f, y, columnWidth, gap);
            Widgets.CheckboxLabeled(rect3, "Count corpse clothes", ref bill.GetAllowDeadmansApparel());
        }
    }
}