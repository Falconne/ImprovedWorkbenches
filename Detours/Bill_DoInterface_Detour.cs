﻿using System.Reflection;
using Harmony;
using RimWorld;
using UnityEngine;
using Verse;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(Bill), "DoInterface")]
    public class Bill_DoInterface_Detour
    {
        private static readonly MethodInfo CanUnpauseGetter = typeof(Bill_Production).GetMethod("CanUnpause",
            BindingFlags.NonPublic | BindingFlags.Instance);

        static bool Prefix(Bill __instance)
        {
            BillStack_DoListing_Detour.BlockButtonDraw = __instance is Bill_Production;

            return true;
        }

        public static void Postfix(ref Bill __instance, float x, float y, float width, int index)
        {
            var billProduction = __instance as Bill_Production;
            if (billProduction == null)
                return;

            Rect rect = new Rect(x, y, width, 53f);
            if (billProduction.paused)
            {
                var extraSize = !(bool) CanUnpauseGetter.Invoke(billProduction, new object[] {}) ? 0f : 24f;
                rect.height += Mathf.Max(17f, extraSize);
            }
            ReorderableWidget.Reorderable(BillStack_DoListing_Detour.ReorderableGroup, rect);
        }

    }
}