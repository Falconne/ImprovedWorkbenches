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
            var billCopyPasteHandler = Main.Instance.BillCopyPasteHandler;
            if (workTable.BillStack != null && workTable.BillStack.Count > 0)
            {
                if (Widgets.ButtonText(rectCopyAll, "Copy All"))
                {
                    billCopyPasteHandler.DoCopy(workTable);
                }
                TooltipHandler.TipRegion(rectCopyAll, "Copy all bills in this workbench");
            }

            if (!billCopyPasteHandler.CanPasteInto(workTable))
                return;

            var rectPaste = new Rect(rectCopyAll);
            rectPaste.xMin += buttonWidth + gap;
            rectPaste.xMax += buttonWidth + gap;
            if (Widgets.ButtonText(rectPaste, 
                billCopyPasteHandler.IsMultipleBillsCopied() ? "Paste All" : "Paste"))
            {
                billCopyPasteHandler.DoPasteInto(workTable, false);
            }
            TooltipHandler.TipRegion(rectPaste, "Paste copied bill(s) as new entries");

            var oldFont = Text.Font;
            Text.Font = GameFont.Tiny;

            var rectLink = new Rect(rectPaste);
            rectLink.xMin += buttonWidth + gap;
            rectLink.xMax += buttonWidth + gap;
            if (Widgets.ButtonText(rectLink, "Paste Link"))
            {
                billCopyPasteHandler.DoPasteInto(workTable, true);
            }
            TooltipHandler.TipRegion(rectLink, 
                "Paste copied bill(s) and link them back to original(s). Changes will be mirrored to all connected bills.");

            Text.Font = oldFont;
        }
    }
}