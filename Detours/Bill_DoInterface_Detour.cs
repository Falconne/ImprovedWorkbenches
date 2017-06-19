using Harmony;
using RimWorld;
using UnityEngine;
using Verse;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(Bill), "DoInterface")]
    public class Bill_DoInterface_Detour
    {
        [HarmonyPostfix]
        public static void Postfix(ref Bill __instance, float y, float width, int index)
        {
            var bill = __instance as Bill_Production;
            if (bill == null)
                return;

            var extendedBillDataStorage = Main.Instance.GetExtendedBillDataStorage();
            var extendedBillData = extendedBillDataStorage.GetExtendedDataFor(bill);
            if (string.IsNullOrEmpty(extendedBillData?.Name))
                return;

            var oldFont = Text.Font;
            Text.Font = GameFont.Small;

            Rect rect = new Rect(28f, y, width - 137f, 24f);
            Widgets.DrawBoxSolid(rect, index % 2 == 0 ? _backgroundColor2 : _backgroundColor1);
            Widgets.Label(rect, extendedBillData.Name);

            Text.Font = oldFont;
        }

        private static Color _backgroundColor1 = new ColorInt(21, 25, 29).ToColor;

        private static Color _backgroundColor2 = new ColorInt(33, 36, 41).ToColor;
    }
}