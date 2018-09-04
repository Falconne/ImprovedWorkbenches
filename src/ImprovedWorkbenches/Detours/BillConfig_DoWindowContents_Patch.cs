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

            if (!ExtendedBillDataStorage.CanOutputBeFiltered(billRaw))
                return;

            if (billRaw.repeatMode != BillRepeatModeDefOf.TargetCount)
                return;

            const float buttonHeight = 26f;

            var y = inRect.height - 84f;

            // Helper method for checkboxes
            void SimpleCheckBoxWithToolTip(string label, ref bool setting, string tip)
            {
                var subRect = new Rect(0f, y, columnWidth, buttonHeight);
                Widgets.CheckboxLabeled(subRect, label.Translate(),
                    ref setting);

                TooltipHandler.TipRegion(subRect, tip.Translate());
                y += 26;
            };

            // Checkbox helper method with consistent language tokens
            void SimpleCheckBox(string label, ref bool setting)
            {
                SimpleCheckBoxWithToolTip($"IW.{label}Label", ref setting, $"IW.{label}Desc");
            }

            // Inventory Filter
            if (billRaw.includeEquipped)
            {
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