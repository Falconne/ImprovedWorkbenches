using HarmonyLib;
using RimWorld;
using System.Reflection;
using UnityEngine;
using Verse;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(Dialog_BillConfig), "DoWindowContents")]
    public static class BillConfig_DoWindowContents_Patch
    {
        private static readonly FieldInfo BillGetter = typeof(Dialog_BillConfig).GetField("bill",
            BindingFlags.NonPublic | BindingFlags.Instance);

        [HarmonyPostfix]
        public static void DrawFilters(Dialog_BillConfig __instance, Rect inRect)
        {
            var billRaw = (Bill_Production)BillGetter.GetValue(__instance);
            if (billRaw == null)
                return;

            var extendedBillDataStorage = Main.Instance.GetExtendedBillDataStorage();
            extendedBillDataStorage.MirrorBillToLinkedBills(billRaw);

            var extendedBillData = extendedBillDataStorage.GetOrCreateExtendedDataFor(billRaw);
            if (extendedBillData == null)
                return;

            // Bill navigation buttons
            DrawWorkTableNavigation(__instance, billRaw, inRect);

            var nextConfigButtonX = inRect.xMin + 65f;

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

            float columnWidth = (inRect.width - 34f) / 3f;
            float buttonHight = Text.CalcHeight("IW.CountAwayLabel".Translate(), columnWidth) + 8f;
            float optionsHeight = buttonHight + 2f + 2f + Text.LineHeight + 2f + 4f + 1f + 8f + Text.LineHeight;
            float optionsOffset = inRect.height - 50f - optionsHeight;

            Listing_Standard optionsList = new Listing_Standard();
            optionsList.Begin(new Rect(0, optionsOffset, columnWidth, optionsHeight));

            // Output Filter
            if (ExtendedBillDataStorage.CanOutputBeFiltered(billRaw) && (billRaw.repeatMode == BillRepeatModeDefOf.TargetCount))
            {
                if (optionsList.ButtonText("IW.OutputFilterLabel".Translate()))
                {
                    Window temp = Find.WindowStack.currentlyDrawnWindow;
                    temp.Close();
                    Find.WindowStack.Add(new Dialog_ThingFilter(extendedBillData, temp));
                }
                optionsList.Gap(2f);
                optionsList.CheckboxLabeled("IW.CountAwayLabel".Translate(), ref extendedBillData.CountAway, "IW.CountAwayDesc".Translate());
                optionsList.Gap(4f);
                optionsList.GapLine(1f);
            }
            else
            {
                optionsList.Gap(buttonHight + optionsList.verticalSpacing);
                optionsList.Gap(2f);
                optionsList.Gap(Text.LineHeight + optionsList.verticalSpacing);
                optionsList.Gap(4f);
                optionsList.Gap(1f);
            }


            optionsList.Gap(8f);

            // Workbench Restriction
            Building_WorkTable workTable = Find.Selector.SingleSelectedThing as Building_WorkTable;
            WorktableRestrictionData workbenchRestrictionData = Find.World.GetComponent<WorktableRestrictionDataStorage>()?.GetWorktableRestrictionData(workTable.thingIDNumber);
            if (workbenchRestrictionData != null)
                optionsList.CheckboxLabeled("IW.WorkbenchRestrictionLabel".Translate(), ref workbenchRestrictionData.isRestricted, "IW.WorkbenchRestrictionDesc".Translate());

            optionsList.End();
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

            void Move(int direction)
            {
                var otherBill = (Bill_Production)billStack.Bills[thisBillIndexInWorkTable + direction];
                dialog.Close();
                Find.WindowStack.Add(new Dialog_BillConfig(otherBill, workTable.Position));
            }

            if (thisBillIndexInWorkTable > 0)
            {
                if (Widgets.ButtonImage(leftRect, Resources.LeftArrow))
                {
                    Move(-1);
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
                    Move(1);
                }
                TooltipHandler.TipRegion(rightRect, "IW.OpenNextBillTip".Translate());
            }
        }
    }
}
