﻿using Harmony;
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
                if (Widgets.ButtonText(rectCopyAll, "IW.CopyAllLabel".Translate()))
                {
                    billCopyPasteHandler.DoCopy(workTable);
                }
                TooltipHandler.TipRegion(rectCopyAll, "IW.CopyAllTip".Translate());
            }

            if (!billCopyPasteHandler.CanPasteInto(workTable))
                return;

            var rectPaste = new Rect(rectCopyAll);
            rectPaste.xMin += buttonWidth + gap;
            rectPaste.xMax += buttonWidth + gap;
            if (Widgets.ButtonText(rectPaste, 
                billCopyPasteHandler.IsMultipleBillsCopied() ? "IW.PasteAllLabel".Translate() : "IW.Paste".Translate()))
            {
                billCopyPasteHandler.DoPasteInto(workTable, false);
            }
            TooltipHandler.TipRegion(rectPaste, "IW.PasteAllTip".Translate());

            var oldFont = Text.Font;
            Text.Font = GameFont.Tiny;

            var rectLink = new Rect(rectPaste);
            rectLink.xMin += buttonWidth + gap;
            rectLink.xMax += buttonWidth + gap;
            if (Widgets.ButtonText(rectLink, "IW.PasteLinkLabel".Translate()))
            {
                billCopyPasteHandler.DoPasteInto(workTable, true);
            }
            TooltipHandler.TipRegion(rectLink,
                "IW.PasteLinkTip".Translate());

            Text.Font = oldFont;
        }
    }
}