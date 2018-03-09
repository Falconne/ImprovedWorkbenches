using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Harmony;
using RimWorld;
using UnityEngine;
using Verse;
using ImprovedWorkbenches.Filtering;

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
                    var smLabel = storeModeDef.LabelCap;
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
                    var label = "IW.TakeToLabel".Translate() + " " + stockpile.label;
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

            var nextConfigButtonX = inRect.xMin + 28f;

            // Copy bill button
            {
                var copyBillRect = new Rect(nextConfigButtonX, inRect.yMin + 50f, 24f, 24f);
                if (Widgets.ButtonImage(copyBillRect, Resources.CopyButton))
                {
                    Main.Instance.BillCopyPasteHandler.DoCopy(billRaw);
                }
                TooltipHandler.TipRegion(copyBillRect, "IW.CopyJustBillsTip".Translate());
                nextConfigButtonX += 28f;
            }

            // Paste into bill button
            {
                var pasteRect = new Rect(nextConfigButtonX, inRect.yMin + 50f, 24f, 24f);
                var copyPasteHandler = Main.Instance.BillCopyPasteHandler;
                if (copyPasteHandler.CanPasteInto(billRaw))
                {
                    if (Widgets.ButtonImage(pasteRect, Resources.PasteButton))
                    {
                        copyPasteHandler.DoPasteInto(billRaw);
                    }
                    TooltipHandler.TipRegion(pasteRect, "IW.PasteBillSettings".Translate());

                    nextConfigButtonX += 28f;
                }

            }

            // Linked bill handling
            if (extendedBillDataStorage.IsLinkedBill(billRaw))
            {
                var unlinkRect = new Rect(nextConfigButtonX, inRect.yMin + 50f, 24f, 24f);
                if (Widgets.ButtonImage(unlinkRect, Resources.BreakLink))
                {
                    extendedBillDataStorage.RemoveBillFromLinkSets(billRaw);
                }
                TooltipHandler.TipRegion(unlinkRect, "IW.BreakLinkToOtherBillsTip".Translate());
            }

            // Bill renaming
            {
                var renameRect = new Rect(inRect.xMax - 75f, inRect.yMin + 4f, 24f, 24f);
                if (Widgets.ButtonImage(renameRect, Resources.Rename))
                {
                    Find.WindowStack.Add(new Dialog_RenameBill(extendedBillData, billRaw.LabelCap));
                }
                TooltipHandler.TipRegion(renameRect, "IW.RenameBillTip".Translate());
            }

            const float columnWidth = 180f;
            const float middleColumn = columnWidth + 34f;

            // Allowed worker filter
            var potentialWorkers = GetAllowedWorkersWithSkillLevel(billRaw);
            if (potentialWorkers != null)
            {
                var anyoneText = "IW.NoColRestrictionLabel".Translate();
                var workerButtonRect = new Rect(middleColumn + 3f, inRect.yMin + 18f,
                    columnWidth, 30f);

                var currentWorkerLabel =
                    extendedBillData.Worker != null
                    ? "IW.RestrictToLabel".Translate() + " " + extendedBillData.Worker.NameStringShort.CapitalizeFirst()
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
                        var nameWithSkill = $"{skillPrefix}" + "IW.RestrictToLabel".Translate() + " " + $"{allowedWorker}";

                        var workerMenuItem = new FloatMenuOption(nameWithSkill,
                            delegate { extendedBillData.Worker = allowedWorker; });

                        potentialWorkerList.Add(workerMenuItem);
                    }

                    Find.WindowStack.Add(new FloatMenu(potentialWorkerList));
                }
                TooltipHandler.TipRegion(workerButtonRect, "IW.RestrictJobToSpecificColonistTip".Translate());
            }

            // Custom take to stockpile, overlay dummy button
            {
                var storeRect = new Rect(middleColumn + 3f, inRect.yMin + 114f,
                    columnWidth, 30f);

                var label = extendedBillData.UsesTakeToStockpile()
                    ? extendedBillData.CurrentTakeToStockpileLabel()
                    : billRaw.storeMode.LabelCap;

                Widgets.ButtonText(storeRect, label);
                TooltipHandler.TipRegion(storeRect,
                    "IW.CrafterWillToSpecificStockpileTip".Translate());
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
                TooltipHandler.TipRegion(keyboardRect, "IW.RenameTip".Translate());
            }

            const float buttonHeight = 26f;
            const float smallButtonHeight = 24f;

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
                TooltipHandler.TipRegion(keyboardRect, "IW.RenameTip".Translate());
            }

            var y = inRect.height - 245f + Text.LineHeight;
            // Restrict counting to specific stockpile
            {
                var subRect = new Rect(0f, y, columnWidth, buttonHeight);
                var anyStockpileText = "IW.CountOnStockpilesText".Translate();
                var currentCountingStockpileLabel = extendedBillData.UsesCountingStockpile()
                    ? "IW.CountInText".Translate() + " " + extendedBillData.GetCountingStockpile().label
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
                        var stockpileName = "IW.CountInText".Translate() + " " + stockpile.label;
                        var menuOption = new FloatMenuOption(
                            stockpileName,
                            delegate { extendedBillData.SetCountingStockpile(stockpile); });

                        potentialStockpileList.Add(menuOption);
                    }

                    Find.WindowStack.Add(new FloatMenu(potentialStockpileList));
                }
                TooltipHandler.TipRegion(subRect,
                    "IW.WillCountTowardsTargetTip".Translate());
            }

            var thingDef = billRaw.recipe.products.First().thingDef;

            if (!thingDef.CountAsResource)
            {
                // Counted items filter
                y += 33;
                var countedLabelRect = new Rect(0f, y, columnWidth, buttonHeight);
                Widgets.Label(countedLabelRect, "IW.CountedItemsFilter".Translate());
                y += Text.LineHeight;

                var filter = extendedBillData.OutputFilter;
                if (filter.allowedHitPointsConfigurable)
                {
                    var allowedHitPointsPercents = filter.AllowedHitPointsPercents;
                    var subRect = new Rect(0f, y, columnWidth, buttonHeight);
                    Widgets.FloatRange(subRect, 10, ref allowedHitPointsPercents, 0f, 1f,
                        "HitPoints", ToStringStyle.PercentZero);

                    TooltipHandler.TipRegion(subRect,
                        "IW.HitPointsTip".Translate());
                    filter.AllowedHitPointsPercents = allowedHitPointsPercents;
                }

                if (filter.allowedQualitiesConfigurable)
                {
                    y += 33;
                    var subRect = new Rect(0f, y, columnWidth, buttonHeight);
                    var allowedQualityLevels = filter.AllowedQualityLevels;
                    Widgets.QualityRange(subRect, 11, ref allowedQualityLevels);
                    TooltipHandler.TipRegion(subRect,
                        "IW.QualityTip".Translate());
                    filter.AllowedQualityLevels = allowedQualityLevels;
                }

            }

            y += 7;

            // Helper method for checkboxes
            void SimpleCheckBoxWithToolTip(string label, ref bool setting, string tip)
            {
                y += 26;
                var subRect = new Rect(0f, y, columnWidth, buttonHeight);
                Widgets.CheckboxLabeled(subRect, label.Translate(),
                    ref setting);

                TooltipHandler.TipRegion(subRect, tip.Translate());
            };

            // Checkbox helper method with consistent language tokens
            void SimpleCheckBox(string label, ref bool setting)
            {
                SimpleCheckBoxWithToolTip($"IW.{label}Label", ref setting, $"IW.{label}Desc");
            }

            // Use input ingredients for counted items filter
            if (billRaw.ingredientFilter != null && thingDef.MadeFromStuff)
                SimpleCheckBoxWithToolTip("IW.MatchInputIngredientsText", ref extendedBillData.UseInputFilter,
                    "IW.IngredientsTip");

            //Installable filter
            if (thingDef.Minifiable)
                SimpleCheckBox("CountInstalled", ref extendedBillData.CountInstalled);

            // Deadmans clothing count filter
            if (thingDef.IsApparel)
            {
                var nonDeadmansApparelFilter = new SpecialThingFilterWorker_NonDeadmansApparel();
                if (nonDeadmansApparelFilter.CanEverMatch(thingDef))
                    SimpleCheckBox("CountCorpseClothes", ref extendedBillData.AllowDeadmansApparel);
            }

            // Inventory Filter
            if (StatFilterWrapper.GoesInInventory(thingDef))
            {
                SimpleCheckBox("CountInventory", ref extendedBillData.CountInventory);
                if (extendedBillData.CountInventory)
                    SimpleCheckBox("CountAway", ref extendedBillData.CountAway);
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
                TooltipHandler.TipRegion(leftRect, "IW.OpenPreviousBillTip".Translate());
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
                TooltipHandler.TipRegion(rightRect, "IW.OpenNextBillTip".Translate());
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