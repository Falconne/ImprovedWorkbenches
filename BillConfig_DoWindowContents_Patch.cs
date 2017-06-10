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
            var rect = new Rect(0f, inRect.height - 211f, columnWidth, 40f);
            var y = rect.yMin + Text.LineHeight - 1;

            // Allowed worker filter
            var potentialWorkers = GetAllowedWorkersWithSkillLevel(billRaw);

            if (potentialWorkers != null)
            {
                Widgets.Label(rect, "Restrict to:");
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

                    foreach (var allowedWorkerAndTheirSkill in potentialWorkers)
                    {
                        var allowedWorker = allowedWorkerAndTheirSkill.First;
                        var level = allowedWorkerAndTheirSkill.Second.Level;
                        string passion;
                        switch (allowedWorkerAndTheirSkill.Second.passion)
                        {
                            case Passion.Minor:
                                passion = "+";
                                break;
                            case Passion.Major:
                                passion = "++";
                                break;
                            default:
                                passion = "";
                                break;
                        }

                        var nameWithSkill = $"[{level}{passion}] {allowedWorker}";
                        nameWithSkill = nameWithSkill.Truncate(columnWidth);

                        var workerMenuItem = new FloatMenuOption(nameWithSkill,
                            delegate { billWithWorkerFilter.SetWorker(allowedWorker); });

                        potentialWorkerList.Add(workerMenuItem);
                    }

                    Find.WindowStack.Add(new FloatMenu(potentialWorkerList));
                }
            }

            // Counted items filter (if applicable)
            if (billRaw.repeatMode != BillRepeatModeDefOf.TargetCount ||
                !BillUtility_Detour.CanOutputBeFiltered(billRaw))
            {
                return;
            }

            // "Unpause when" level adjustment buttons
            if (billRaw.pauseWhenSatisfied)
            {
                var sectionLeft = columnWidth + 34f;
                var buttonWidth = 42f;
                var buttonHeight = 24f;
                var minusOneRect = new Rect(sectionLeft, inRect.height - 70, buttonWidth, buttonHeight);
                if (Widgets.ButtonText(minusOneRect, "-1"))
                {
                    if (billRaw.unpauseWhenYouHave > 0)
                    {
                        billRaw.unpauseWhenYouHave--;
                    }
                }

                var plusOneRect = new Rect(minusOneRect);
                plusOneRect.xMin += buttonWidth + 2f;
                plusOneRect.xMax += buttonWidth + 2f;
                if (Widgets.ButtonText(plusOneRect, "+1"))
                {
                    if (billRaw.unpauseWhenYouHave < billRaw.targetCount - 1)
                    {
                        billRaw.unpauseWhenYouHave++;
                    }
                }
            }

            y += 33;
            var countedLabelRect = new Rect(0f, y, columnWidth, gap);
            Widgets.Label(countedLabelRect, "Counted items filter:");
            y += Text.LineHeight;

            var billWithThingFilter = billRaw as IBillWithThingFilter;
            // This won't be null, if we got here.
            // ReSharper disable once PossibleNullReferenceException
            var filter = billWithThingFilter.GetOutputFilter();
            if (filter.allowedHitPointsConfigurable)
            {
                var allowedHitPointsPercents = filter.AllowedHitPointsPercents;
                var rect1 = new Rect(0f, y, columnWidth, gap);
                Widgets.FloatRange(rect1, 10, ref allowedHitPointsPercents, 0f, 1f,
                    "HitPoints", ToStringStyle.PercentZero);
                filter.AllowedHitPointsPercents = allowedHitPointsPercents;
            }

            if (!filter.allowedQualitiesConfigurable)
                return;

            y += 33;
            var rect2 = new Rect(0f, y, columnWidth, gap);
            var allowedQualityLevels = filter.AllowedQualityLevels;
            Widgets.QualityRange(rect2, 11, ref allowedQualityLevels);
            filter.AllowedQualityLevels = allowedQualityLevels;


            var thingDef = billRaw.recipe.products.First().thingDef;
            // Use input ingredients for counted items filter
            if (billRaw.ingredientFilter != null && thingDef.MadeFromStuff)
            {
                y += 35;
                var subRect = new Rect(0f, y, columnWidth, gap);
                Widgets.CheckboxLabeled(subRect, "Match input ingredients",
                    ref billWithThingFilter.GetUseInputFilter());
            }


            // Deadmans clothing count filter
            var nonDeadmansApparelFilter = new SpecialThingFilterWorker_NonDeadmansApparel();
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

        private static IEnumerable<Pair<Pawn, SkillRecord>> GetAllowedWorkersWithSkillLevel(
            Bill bill)
        {
            var thing = bill.billStack?.billGiver as Thing;
            if (thing == null)
                return null;

            var workSkill = bill.recipe?.workSkill;
            if (workSkill == null)
                return null;

            var allDefsListForReading = DefDatabase<WorkGiverDef>.AllDefsListForReading;

            var workTypeDef = allDefsListForReading.FirstOrDefault(t =>
                t.fixedBillGiverDefs != null && t.fixedBillGiverDefs.Contains(thing.def))?.workType;

            if (workTypeDef == null)
                return null;

            var validPawns = Find.VisibleMap.mapPawns.FreeColonists.Where(
                p => p.workSettings.WorkIsActive(workTypeDef));

            var pawnsWithTheirSkill = validPawns.Select(
                p => new Pair<Pawn, SkillRecord>(p, p.skills.GetSkill(workSkill)));

            var maxPassion = Enum.GetNames(typeof(Passion)).Length + 1f;

            var pawnsOrderedBySkill = pawnsWithTheirSkill.OrderByDescending(pws => 
                pws.Second.Level + (int) pws.Second.passion / maxPassion);

            return pawnsOrderedBySkill;
        }
    }
}