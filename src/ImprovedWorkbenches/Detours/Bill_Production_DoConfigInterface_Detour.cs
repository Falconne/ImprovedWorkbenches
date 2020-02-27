using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(Bill_Production), "DoConfigInterface")]
    public static class Bill_Production_DoConfigInterface_Detour
    {
        public static bool Prefix()
        {
            BillStack_DoListing_Detour.BlockButtonDraw = false;
            return true;
        }

        public static void Postfix(Bill_Production __instance, Rect baseRect, Color baseColor)
        {
            var storeModeImage = Resources.BestStockpile;
            var nextStoreMode = BillStoreModeDefOf.DropOnFloor;
            var tip = "IW.ClickToDropTip".Translate();
            if (__instance.GetStoreMode() == BillStoreModeDefOf.DropOnFloor)
            {
                storeModeImage = Resources.DropOnFloor;
                nextStoreMode = BillStoreModeDefOf.BestStockpile;
                tip = "IW.ClickToTakeToStockpileTip".Translate();
            }

            var extendedBillDataStorage = Main.Instance.GetExtendedBillDataStorage();
            var storeModeRect = new Rect(baseRect.xMax - 110f, baseRect.y, 24f, 24f);
            if (Widgets.ButtonImage(storeModeRect, storeModeImage, baseColor))
            {
                __instance.SetStoreMode(nextStoreMode);
            }
            TooltipHandler.TipRegion(storeModeRect, tip);

            var nextButtonX = storeModeRect.xMin - 28f;
            var copyPasteHandler = Main.Instance.BillCopyPasteHandler;
            var pasteRect = new Rect(nextButtonX, baseRect.y, 24f, 24f);
            if (copyPasteHandler.CanPasteInto(__instance))
            {
                if (Widgets.ButtonImage(pasteRect, Resources.PasteButton, baseColor))
                {
                    copyPasteHandler.DoPasteInto(__instance);
                }
                TooltipHandler.TipRegion(pasteRect, "IW.PasteBillSettings".Translate());

                nextButtonX -= 28f;
            }

            var breakLinkRect = new Rect(nextButtonX, baseRect.y, 24f, 24f);
            if (extendedBillDataStorage.IsLinkedBill(__instance))
            {
                if (Widgets.ButtonImage(breakLinkRect, Resources.BreakLink, baseColor))
                {
                    extendedBillDataStorage.RemoveBillFromLinkSets(__instance);
                }
                TooltipHandler.TipRegion(breakLinkRect, "IW.BreakLinkToOtherBillsTip".Translate());
            }
        }
    }
}
