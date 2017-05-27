using Harmony;
using RimWorld;
using UnityEngine;
using Verse;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(Dialog_BillConfig), "DoWindowContents")]
    public static class BillConfig_DoWindowContents_Patch
    {
        [HarmonyPostfix]
        public static void DrawHelloButton(Dialog_BillConfig __instance, Rect inRect)
        {
            var buttonSize = new Vector2(120f, 40f);
            if (Widgets.ButtonText(new Rect(0, inRect.height - buttonSize.y, buttonSize.x, buttonSize.y), "FooBar"))
            {
                // do stuff
            }
        }
    }
}