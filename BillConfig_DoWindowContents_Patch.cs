using System;
using System.Collections.Generic;
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
            var billWithWorkerFilter = billRaw as IBillWithWorkerFilter;
            if (billWithWorkerFilter == null)
                // Not one of our controlled Bills
                return;

            const float columnWidth = 180f;
            const float gap = 26f;
            var rect = new Rect(0f, inRect.height - 300f, columnWidth, 40f);

            // Assigned worker filter
            Widgets.Label(rect, "Worker:");
            var y = rect.yMin + Text.LineHeight - 1;

            var workerButtonRect = new Rect(0f, y, columnWidth, gap);

            var currentWorkerLabel = 
                billWithWorkerFilter.GetWorker()?.NameStringShort.CapitalizeFirst().Truncate(columnWidth) ??
                "Anybody";

            if (Widgets.ButtonText(workerButtonRect, currentWorkerLabel))
            {
                var potentialWorkerList = new List<FloatMenuOption>
                {
                    new FloatMenuOption(
                        "Anybody", delegate { billWithWorkerFilter.SetWorker(null); })
                };

                var potentialWorkers = GetSortedAllowedWorkers(billWithWorkerFilter.GetBillGiver());
                if (potentialWorkers != null)
                {
                    foreach (var allowedWorker in potentialWorkers)
                    {
                        var workerMenuItem = new FloatMenuOption(allowedWorker.NameStringShort,
                            delegate { billWithWorkerFilter.SetWorker(allowedWorker); });

                        potentialWorkerList.Add(workerMenuItem);
                    }
                    
                }
                Find.WindowStack.Add(new FloatMenu(potentialWorkerList));
            }

            // Counted items filter (if applicable)
            if (billRaw.repeatMode != BillRepeatModeDefOf.TargetCount)
                return;

            y += 33;
            var countedLabelRect = new Rect(0f, y, columnWidth, gap);
            Widgets.Label(countedLabelRect, "Counted items filter:");
            y += Text.LineHeight;

            var billWithThingFilter = billRaw as IBillWithThingFilter;
            // This won't be null, if we got here.
            // ReSharper disable once PossibleNullReferenceException
            var filter = billWithThingFilter.GetOutputFilter();
            var allowedHitPointsPercents = filter.AllowedHitPointsPercents;
            var rect1 = new Rect(0f, y, columnWidth, gap);
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
            var thingDef = billWithThingFilter.GetRecipeDef().products.First().thingDef;
            if (!nonDeadmansApparelFilter.CanEverMatch(thingDef))
            {
                // Not apparel, so deadman check is not needed.
                return;
            }
            y += 35;
            var rect3 = new Rect(0f, y, columnWidth, gap);
            Widgets.CheckboxLabeled(rect3, "Count corpse clothes", 
                ref billWithThingFilter.GetAllowDeadmansApparel());
        }

        private static IEnumerable<Pawn> GetSortedAllowedWorkers(IBillGiver billGiver)
        {
            var thing = billGiver as Thing;
            if (thing == null)
            {
                return null;
            }

            var allDefsListForReading = DefDatabase<WorkGiverDef>.AllDefsListForReading;

            var workTypeDef = allDefsListForReading.FirstOrDefault(t =>
                t.fixedBillGiverDefs != null && t.fixedBillGiverDefs.Contains(thing.def))?.workType;

            if (workTypeDef == null)
            {
                Main.Instance.Logger.Warning("workTypeDef is null");
                return null;
            }

            var validPawns = Find.VisibleMap.mapPawns.FreeColonists.Where(
                p => p.workSettings.WorkIsActive(workTypeDef));

            return validPawns;
        }
    }
}