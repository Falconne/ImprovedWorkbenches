﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Harmony;
using RimWorld;
using UnityEngine;
using Verse;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(Bill_Production), "DoConfigInterface")]
    public static class Bill_Production_DoConfigInterface_Detour
    {
        [HarmonyPostfix]
        public static void Postfix(Bill_Production __instance, Rect baseRect, Color baseColor)
        {
            var storeModeImage = Resources.BestStockpile;
            var nextStoreMode = BillStoreModeDefOf.DropOnFloor;
            var tip = "IW.ClickToDropTip".Translate();
            if (__instance.storeMode == BillStoreModeDefOf.DropOnFloor)
            {
                storeModeImage = Resources.DropOnFloor;
                nextStoreMode = BillStoreModeDefOf.BestStockpile;
                tip = "IW.ClickToTakeToStockpileTip".Translate();
            }

            var extendedBillDataStorage = Main.Instance.GetExtendedBillDataStorage();
            var storeModeRect = new Rect(baseRect.xMax - 80f, baseRect.y, 24f, 24f);
            if (Widgets.ButtonImage(storeModeRect, storeModeImage, baseColor))
            {
                __instance.storeMode = nextStoreMode;
                var extendedBillData = extendedBillDataStorage.GetExtendedDataFor(__instance);
                extendedBillData?.RemoveTakeToStockpile();
            }
            TooltipHandler.TipRegion(storeModeRect, tip);

            var copyBillRect = new Rect(storeModeRect.xMin - 28f, baseRect.y, 24f, 24f);
            if (Widgets.ButtonImage(copyBillRect, Resources.CopyButton, baseColor))
            {
                Main.Instance.BillCopyPasteHandler.DoCopy(__instance);
            }
			TooltipHandler.TipRegion(copyBillRect, "IW.CopyJustBillsTip".Translate());

            if (!extendedBillDataStorage.IsLinkedBill(__instance))
                return;

            var breakLinkRect = new Rect(copyBillRect.xMin - 28f, baseRect.y, 24f, 24f);
            if (Widgets.ButtonImage(breakLinkRect, Resources.BreakLink, baseColor))
            {
                extendedBillDataStorage.RemoveBillFromLinkSets(__instance);
            }
            TooltipHandler.TipRegion(breakLinkRect, "IW.BreakLinkToOtherBillsTip".Translate());
        }
    }
}
