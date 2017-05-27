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
            var bill = (Bill_Production)AccessTools.Field(typeof(Dialog_BillConfig), "bill").GetValue(__instance);
            if (bill.repeatMode != BillRepeatModeDefOf.TargetCount)
                return;

            if (!(bill.recipe.products != null &&
                  bill.recipe.products.Count > 0 &&
                  bill.recipe.products.First().thingDef.BaseMarketValue > 0))
            {
                return;
            }

            var thingDef = bill.recipe.products.First().thingDef;
            var thingFilter = new ThingFilter();
            thingFilter.SetDisallowAll();
            thingFilter.SetAllow(thingDef, true);

            var rect = new Rect(0, inRect.height - 200f, 160f, 40f);
            Widgets.Label(rect, "Counted items filter:");
            var row = inRect.height - 150f;
            DrawHitPointsFilterConfig(ref row, rect.width, thingFilter);

        }

        private static void DrawHitPointsFilterConfig(ref float y, float width, ThingFilter filter)
        {
            if (!filter.allowedHitPointsConfigurable)
            {
                return;
            }
            Rect rect = new Rect(0f, y, width, 26f);
            FloatRange allowedHitPointsPercents = filter.AllowedHitPointsPercents;
            Widgets.FloatRange(rect, 1, ref allowedHitPointsPercents, 0f, 1f, "HitPoints", ToStringStyle.PercentZero);
            filter.AllowedHitPointsPercents = allowedHitPointsPercents;
            y += 26f;
            y += 5f;
            Text.Font = GameFont.Small;
        }
    }
}