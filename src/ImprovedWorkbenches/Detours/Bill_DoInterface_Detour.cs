using System.Reflection;
using HarmonyLib;
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
            if (!Main.Instance.ShouldAllowDragToReorder())
                return true;

            BillStack_DoListing_Detour.BlockButtonDraw = __instance is Bill_Production;

            return true;
        }

        public static void Postfix(ref Bill __instance, float x, float y, float width, int index)
        {
            if (!Main.Instance.ShouldAllowDragToReorder())
                return;

            if (!(__instance is Bill_Production billProduction))
                return;

            var rect = new Rect(x, y, width, 53f);
            if (billProduction.paused)
            {
                var extraSize = !(bool) CanUnpauseGetter.Invoke(billProduction, new object[] {}) ? 0f : 24f;
                rect.height += Mathf.Max(17f, extraSize);
            }
            ReorderableWidget.Reorderable(BillStack_DoListing_Detour.ReorderableGroup, rect);

            var dragRect = new Rect(x, y + 12f, 24f, 24f);
            TooltipHandler.TipRegion(dragRect, "DragToReorder".Translate());
            GUI.DrawTexture(dragRect, Resources.DragHash);
        }

    }
}