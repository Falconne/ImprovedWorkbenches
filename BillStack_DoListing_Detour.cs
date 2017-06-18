using Harmony;
using RimWorld;
using UnityEngine;
using Verse;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(BillStack), "DoListing")]
    public class BillStack_DoListing_Detour
    {
        [HarmonyPostfix]
        public static void Postfix(ref BillStack __instance, ref Rect rect)
        {
            var workTable = __instance.billGiver as Building_WorkTable;
            if (workTable == null)
                return;

            var gap = 4f;
            var buttonWidth = 70f;
            var rectCopyAll = new Rect(rect.xMin + 154f, rect.yMin, buttonWidth, 29f);
            if (Widgets.ButtonText(rectCopyAll, "Copy All"))
            {
                Main.Instance.BillCopyPasteHandler.DoCopy(workTable);
            }
            TooltipHandler.TipRegion(rectCopyAll, "Copy all bills in this workbench");

            if (!Main.Instance.BillCopyPasteHandler.CanPasteInto(workTable))
                return;

            var rectPaste = new Rect(rectCopyAll);
            rectPaste.xMin += buttonWidth + gap;
            rectPaste.xMax += buttonWidth + gap;
            if (Widgets.ButtonText(rectPaste, "Paste"))
            {
                Main.Instance.BillCopyPasteHandler.DoPasteInto(workTable, false);
            }
            TooltipHandler.TipRegion(rectPaste, "Paste copied bills as new entries");

            var oldFont = Text.Font;
            Text.Font = GameFont.Tiny;

            var rectLink = new Rect(rectPaste);
            rectLink.xMin += buttonWidth + gap;
            rectLink.xMax += buttonWidth + gap;
            if (Widgets.ButtonText(rectLink, "Paste Link"))
            {
                Main.Instance.BillCopyPasteHandler.DoPasteInto(workTable, true);
            }
            TooltipHandler.TipRegion(rectLink, "Paste copied bills and link them back to the originals");

            Text.Font = oldFont;
        }
    }
}