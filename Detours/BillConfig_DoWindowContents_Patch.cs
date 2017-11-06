using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using RimWorld;
using UnityEngine;
using Verse;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(Dialog_BillConfig), "DoWindowContents")]
    public static class BillConfig_DoWindowContents_Patch
    {
        private static readonly FieldInfo BillGetter = typeof(Dialog_BillConfig).GetField("bill",
            BindingFlags.NonPublic | BindingFlags.Instance);

        [HarmonyPrefix]
        public static bool Prefix(Dialog_BillConfig __instance, Rect inRect)
        {
            if (!(BillGetter.GetValue(__instance) is Bill_Production billRaw))
                return true;

            ShowCustomTakeToStockpileMenu(billRaw, inRect);

            Main.Instance.OnProductionDialogBeingShown();

            return true;
        }

        // Specific storage stockpile
        private static void ShowCustomTakeToStockpileMenu(Bill_Production billRaw, Rect inRect)
        {
            var extendedBillDataStorage = Main.Instance.GetExtendedBillDataStorage();
            var extendedBillData = extendedBillDataStorage.GetExtendedDataFor(billRaw);
            if (extendedBillData == null)
                return;

            const float columnWidth = 180f;
            const float middleColumn = columnWidth + 34f;
            const float buttonHeight = 30f;

            var storeRect = new Rect(middleColumn + 3f, inRect.yMin + 114f,
                columnWidth, buttonHeight);
            var allStockpiles = Find.VisibleMap.zoneManager.AllZones.OfType<Zone_Stockpile>();

            if (Widgets.ButtonText(storeRect, "null"))
            {
                var storeOptionList = new List<FloatMenuOption>();

                var builtInStoremodesQry =
                    from bsm in DefDatabase<BillStoreModeDef>.AllDefs
                    orderby bsm.listOrder
                    select bsm;

                foreach (var storeModeDef in builtInStoremodesQry)
                {
                    var smLocal = storeModeDef;
                    var smLabel = ("BillStoreMode_" + storeModeDef).Translate();
                    storeOptionList.Add(
                        new FloatMenuOption(
                            smLabel,
                            delegate
                            {
                                billRaw.storeMode = smLocal;
                                extendedBillData.RemoveTakeToStockpile();
                            }
                        )
                    );
                }

                foreach (Zone_Stockpile stockpile in allStockpiles)
                {
                    var label = "Take to " + stockpile.label;
                    var option = new FloatMenuOption(
                        label,
                        delegate
                        {
                            extendedBillData.SetTakeToStockpile(stockpile);
                            billRaw.storeMode = BillStoreModeDefOf.BestStockpile;
                        }
                    );

                    storeOptionList.Add(option);
                }

                Find.WindowStack.Add(new FloatMenu(storeOptionList));
            }
        }

        [HarmonyPostfix]
        public static void DrawFilters(Dialog_BillConfig __instance, Rect inRect)
        {
            var billRaw = (Bill_Production)BillGetter.GetValue(__instance);
            if (billRaw == null)
                return;

            Main.Instance.IsRootBillFilterBeingDrawn = false;

            var extendedBillDataStorage = Main.Instance.GetExtendedBillDataStorage();
            extendedBillDataStorage.MirrorBillToLinkedBills(billRaw);

            var extendedBillData = extendedBillDataStorage.GetExtendedDataFor(billRaw);
            if (extendedBillData == null)
                return;

            // Bill navigation buttons
            DrawWorkTableNavigation(__instance, billRaw, inRect);

            // Linked bill handling
            if (extendedBillDataStorage.IsLinkedBill(billRaw))
            {
                var unlinkRect = new Rect(inRect.xMin + 28f, inRect.yMin + 50f, 24f, 24f);
                if (Widgets.ButtonImage(unlinkRect, Resources.BreakLink))
                {
                    extendedBillDataStorage.RemoveBillFromLinkSets(billRaw);
                }
                TooltipHandler.TipRegion(unlinkRect, "Break link to other bills");
            }

            // Bill renaming
            {
                var renameRect = new Rect(inRect.xMax - 75f, inRect.yMin + 4f, 24f, 24f);
                if (Widgets.ButtonImage(renameRect, Resources.Rename))
                {
                    Find.WindowStack.Add(new Dialog_RenameBill(extendedBillData, billRaw.LabelCap));
                }
                TooltipHandler.TipRegion(renameRect, "Rename bill (use empty string to reset)");
            }

            const float columnWidth = 180f;
            const float middleColumn = columnWidth + 34f;

            // Allowed worker filter
            var potentialWorkers = GetAllowedWorkersWithSkillLevel(billRaw);
            if (potentialWorkers != null)
            {
                var anyoneText = "No colonist restriction";
                var workerButtonRect = new Rect(middleColumn + 3f, inRect.yMin + 18f,
                    columnWidth, 30f);

                var currentWorkerLabel =
                    extendedBillData.Worker != null
                    ? "Restrict to " + extendedBillData.Worker.NameStringShort.CapitalizeFirst()
                    : anyoneText;

                if (Widgets.ButtonText(workerButtonRect, currentWorkerLabel))
                {
                    var potentialWorkerList = new List<FloatMenuOption>
                    {
                        new FloatMenuOption(
                            anyoneText, delegate { extendedBillData.Worker = null; })
                    };

                    foreach (var allowedWorkerAndTheirSkill in potentialWorkers)
                    {
                        var allowedWorker = allowedWorkerAndTheirSkill.First;
                        var skillRecord = allowedWorkerAndTheirSkill.Second;
                        var skillPrefix = "";
                        if (skillRecord != null)
                        {
                            var level = skillRecord.Level;
                            string passion;
                            switch (skillRecord.passion)
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
                            skillPrefix = $"[{level}{passion}] ";
                        }
                        var nameWithSkill = $"{skillPrefix} Restrict to {allowedWorker}";

                        var workerMenuItem = new FloatMenuOption(nameWithSkill,
                            delegate { extendedBillData.Worker = allowedWorker; });

                        potentialWorkerList.Add(workerMenuItem);
                    }

                    Find.WindowStack.Add(new FloatMenu(potentialWorkerList));
                }
                TooltipHandler.TipRegion(workerButtonRect, "Restrict job to specific colonist");
            }

            // Custom take to stockpile, overlay dummy button
            {
                var storeRect = new Rect(middleColumn + 3f, inRect.yMin + 114f,
                    columnWidth, 30f);

                var label = extendedBillData.UsesTakeToStockpile()
                    ? extendedBillData.CurrentTakeToStockpileLabel()
                    : ("BillStoreMode_" + billRaw.storeMode).Translate();

                Widgets.ButtonText(storeRect, label);
                TooltipHandler.TipRegion(storeRect,
                    "Crafter will take final product to specified stockpile");
            }

            // Filter copy/paste buttons
            if (billRaw.ingredientFilter != null)
            {
                const float filterButtonWidth = 96f;
                const float filterButtonHeight = 24f;
                var oldFont = Text.Font;
                Text.Font = GameFont.Tiny;

                var copyPasteHandler = Main.Instance.BillCopyPasteHandler;
                var copyButtonRect = new Rect(inRect.xMax - filterButtonWidth * 2 + 4f, inRect.yMax - 35f,
                    filterButtonWidth, filterButtonHeight);

                var parentFilter = billRaw.recipe?.fixedIngredientFilter;
                if (Widgets.ButtonText(copyButtonRect, "Copy Filter"))
                {
                    copyPasteHandler.CopyFilter(billRaw.ingredientFilter, parentFilter);
                }
                TooltipHandler.TipRegion(copyButtonRect, "Copy ingredients filter settings, for pasting into a matching filter");

                if (copyPasteHandler.IsMatchingFilterCopied(parentFilter))
                {
                    var pasteButtonRect = new Rect(copyButtonRect);
                    pasteButtonRect.xMin += filterButtonWidth + 4f;
                    pasteButtonRect.xMax += filterButtonWidth + 4f;
                    if (Widgets.ButtonText(pasteButtonRect, "Paste Filter"))
                    {
                        copyPasteHandler.PasteCopiedFilterInto(billRaw.ingredientFilter);
                    }
                }

                Text.Font = oldFont;
            }

            if (!ExtendedBillDataStorage.CanOutputBeFiltered(billRaw))
                return;

            if (billRaw.repeatMode != BillRepeatModeDefOf.TargetCount)
                return;

            {
                var keyboardRect = new Rect(middleColumn + 90f, inRect.yMin + 208f, 24f, 24f);
                void TargetCountSetter(int i)
                {
                    billRaw.targetCount = i;
                    if (billRaw.unpauseWhenYouHave >= billRaw.targetCount)
                        billRaw.unpauseWhenYouHave = billRaw.targetCount - 1;
                }

                if (Widgets.ButtonImage(keyboardRect, Resources.Rename))
                {
                    Find.WindowStack.Add(new Dialog_NumericEntry(
                        billRaw.targetCount,
                        i => i > 0,
                        TargetCountSetter));
                }
                TooltipHandler.TipRegion(keyboardRect, "Click to enter a value using keyboard");
            }

            const float buttonHeight = 26f;
            const float smallButtonHeight = 24f;
            var y = inRect.height - 248f + Text.LineHeight;

            // "Unpause when" level adjustment buttons
            if (billRaw.pauseWhenSatisfied)
            {
                var buttonWidth = 42f;
                var minusOneRect = new Rect(middleColumn, inRect.height - 70, buttonWidth, smallButtonHeight);
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

                var keyboardRect = new Rect(plusOneRect.xMax + 2f, plusOneRect.yMin, 24f, 24f);
                if (Widgets.ButtonImage(keyboardRect, Resources.Rename))
                {
                    Find.WindowStack.Add(new Dialog_NumericEntry(
                        billRaw.unpauseWhenYouHave,
                        i => i < billRaw.targetCount,
                        i => billRaw.unpauseWhenYouHave = i));
                }
                TooltipHandler.TipRegion(keyboardRect, "Click to enter a value using keyboard");
            }

            // Restrict counting to specific stockpile
            {
                y += 33;
                var subRect = new Rect(0f, y, columnWidth, buttonHeight);
                var anyStockpileText = "Count in all stockpiles";
                var currentCountingStockpileLabel = extendedBillData.UsesCountingStockpile()
                    ? "Count in " + extendedBillData.GetCountingStockpile().label
                    : anyStockpileText;

                var map = Find.VisibleMap;
                var allStockpiles =
                    map.zoneManager.AllZones.OfType<Zone_Stockpile>().ToList();

                if (Widgets.ButtonText(subRect, currentCountingStockpileLabel))
                {
                    var potentialStockpileList = new List<FloatMenuOption>
                    {
                        new FloatMenuOption(
                            anyStockpileText, delegate { extendedBillData.RemoveCountingStockpile(); })
                    };

                    foreach (var stockpile in allStockpiles)
                    {
                        var stockpileName = "Count in " + stockpile.label;
                        var menuOption = new FloatMenuOption(
                            stockpileName,
                            delegate { extendedBillData.SetCountingStockpile(stockpile); });

                        potentialStockpileList.Add(menuOption);
                    }

                    Find.WindowStack.Add(new FloatMenu(potentialStockpileList));
                }
                TooltipHandler.TipRegion(subRect,
                    "Only items in specified stockpile will count towards target");
            }

            var thingDef = billRaw.recipe.products.First().thingDef;

            if (!thingDef.CountAsResource)
            {
                // Counted items filter
                y += 33;
                var countedLabelRect = new Rect(0f, y, columnWidth, buttonHeight);
                Widgets.Label(countedLabelRect, "Counted items filter:");
                y += Text.LineHeight;

                var filter = extendedBillData.OutputFilter;
                if (filter.allowedHitPointsConfigurable)
                {
                    var allowedHitPointsPercents = filter.AllowedHitPointsPercents;
                    var rect1 = new Rect(0f, y, columnWidth, buttonHeight);
                    Widgets.FloatRange(rect1, 10, ref allowedHitPointsPercents, 0f, 1f,
                        "HitPoints", ToStringStyle.PercentZero);

                    TooltipHandler.TipRegion(rect1,
                        "Only items with given hitpoints range will count towards target");
                    filter.AllowedHitPointsPercents = allowedHitPointsPercents;
                }

                if (filter.allowedQualitiesConfigurable)
                {
                    y += 33;
                    var rect2 = new Rect(0f, y, columnWidth, buttonHeight);
                    var allowedQualityLevels = filter.AllowedQualityLevels;
                    Widgets.QualityRange(rect2, 11, ref allowedQualityLevels);
                    TooltipHandler.TipRegion(rect2,
                        "Only items of given quality range will count towards target");
                    filter.AllowedQualityLevels = allowedQualityLevels;
                }
                
            }

            // Use input ingredients for counted items filter
            if (billRaw.ingredientFilter != null && thingDef.MadeFromStuff)
            {
                y += 33;
                var subRect = new Rect(0f, y, columnWidth, buttonHeight);
                Widgets.CheckboxLabeled(subRect, "Match input ingredients",
                    ref extendedBillData.UseInputFilter);

                TooltipHandler.TipRegion(subRect,
                    "Only items made from ingredients in input ingredients filter (on rightmost column) will count towards target");
            }


            // Deadmans clothing count filter
            if (thingDef.IsApparel)
            {
                var nonDeadmansApparelFilter = new SpecialThingFilterWorker_NonDeadmansApparel();
                if (!nonDeadmansApparelFilter.CanEverMatch(thingDef))
                {
                    // Thing can't be worn.
                    return;
                }

                y += 26;
                var rect3 = new Rect(0f, y, columnWidth, buttonHeight);
                Widgets.CheckboxLabeled(rect3, "Count corpse clothes",
                    ref extendedBillData.AllowDeadmansApparel);
                TooltipHandler.TipRegion(rect3,
                    "Include corpse worn clothing in item count");

                y += 26;
                var rect4 = new Rect(0f, y, columnWidth, buttonHeight);
                Widgets.CheckboxLabeled(rect4, "Count equipped clothes",
                    ref extendedBillData.CountWornApparel);
                TooltipHandler.TipRegion(rect4,
                    "Also count clothes currently worn by colonists");
            }
        }

        private static void DrawWorkTableNavigation(Dialog_BillConfig dialog, Bill_Production bill, Rect inRect)
        {
            var workTable = Find.Selector.SingleSelectedThing as Building_WorkTable;
            var billStack = workTable?.BillStack;
            if (billStack == null || billStack.Count < 2)
                return;

            const float buttonWidth = 14f;
            const float xOffset = 10f + 2 * buttonWidth;
            var leftRect = new Rect(inRect.xMax - xOffset, inRect.yMin + 4f, buttonWidth, 24f);
            var thisBillIndexInWorkTable = billStack.Bills.FirstIndexOf(b => b == bill);

            Action<int> mover = direction =>
            {
                var otherBill = (Bill_Production)billStack.Bills[thisBillIndexInWorkTable + direction];
                dialog.Close();
                Find.WindowStack.Add(new Dialog_BillConfig(otherBill, workTable.Position));
            };

            if (thisBillIndexInWorkTable > 0)
            {
                if (Widgets.ButtonImage(leftRect, Resources.LeftArrow))
                {
                    mover(-1);
                }
                TooltipHandler.TipRegion(leftRect, "Open previous bill in workbench");
            }

            if (thisBillIndexInWorkTable < billStack.Count - 1)
            {
                var rightRect = new Rect(leftRect);
                rightRect.xMin += 4f + buttonWidth;
                rightRect.xMax += 4f + buttonWidth;
                if (Widgets.ButtonImage(rightRect, Resources.RightArrow))
                {
                    mover(1);
                }
                TooltipHandler.TipRegion(rightRect, "Open next bill in workbench");
            }
        }

        private static IEnumerable<Pair<Pawn, SkillRecord>> GetAllowedWorkersWithSkillLevel(
            Bill bill)
        {
            var validPawns = Find.VisibleMap.mapPawns.FreeColonists;
            var thing = bill.billStack?.billGiver as Thing;
            if (thing == null)
                return GetPawnsForUnskilledJob(validPawns);

            var allDefsListForReading = DefDatabase<WorkGiverDef>.AllDefsListForReading;

            var workTypeDef = allDefsListForReading.FirstOrDefault(t =>
                t.fixedBillGiverDefs != null && t.fixedBillGiverDefs.Contains(thing.def))?.workType;

            if (workTypeDef == null)
                return GetPawnsForUnskilledJob(validPawns);

            validPawns = Find.VisibleMap.mapPawns.FreeColonists.Where(
                p => p.workSettings.WorkIsActive(workTypeDef));

            var workSkill = bill.recipe?.workSkill;
            if (workSkill == null)
                return GetPawnsForUnskilledJob(validPawns);

            var pawnsWithTheirSkill = validPawns.Select(
                p => new Pair<Pawn, SkillRecord>(p, p.skills.GetSkill(workSkill)));

            var maxPassion = Enum.GetNames(typeof(Passion)).Length + 1f;

            var pawnsOrderedBySkill = pawnsWithTheirSkill.OrderByDescending(pws =>
                pws.Second.Level + (int)pws.Second.passion / maxPassion);

            return pawnsOrderedBySkill;
        }

        private static IEnumerable<Pair<Pawn, SkillRecord>> GetPawnsForUnskilledJob(IEnumerable<Pawn> validPawns)
        {
            return validPawns.Select(p => new Pair<Pawn, SkillRecord>(p, null));
        }
    }
}