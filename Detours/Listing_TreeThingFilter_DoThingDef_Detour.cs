using System.Linq;
using Harmony;
using UnityEngine;
using Verse;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(Listing_TreeThingFilter), "DoThingDef")]
    public class Listing_TreeThingFilter_DoThingDef_Detour
    {
        [HarmonyPostfix]
        public static void Postfix(Listing_TreeThingFilter __instance, ThingDef tDef)
        {
            if (!Main.Instance.IsRootBillFilterBeingDrawn)
                return;

            var map = Find.VisibleMap;
            var stuffCount = tDef.CountAsResource
                ? map.resourceCounter.GetCount(tDef)
                : map.listerThings.ThingsOfDef(tDef).Count;

            if (stuffCount == 0)
                return;

            var readjustedY = __instance.CurHeight - __instance.lineHeight - __instance.verticalSpacing;
            var rect = new Rect(__instance.ColumnWidth - 100f, readjustedY, 40f, __instance.lineHeight);
            GUI.color = Color.gray;
            Widgets.Label(rect, stuffCount.ToString().PadLeft(4, ' '));
            GUI.color = Color.white;
        }
    }
}