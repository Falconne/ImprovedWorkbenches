using System.Linq;
using Harmony;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(MainTabWindow_Inspect), "ExtraOnGUI")]
    public static class MainTabWindow_Inspect_Detour
    {
        [HarmonyPostfix]
        public static void Postfix(MainTabWindow_Inspect __instance)
        {
            var tab = __instance.CurTabs?.FirstOrDefault();

            if (!(tab is ITab_Bills))
            {
                _lastSelectedThingId = null;
                return;
            }

            var selectedThing = Find.Selector.SingleSelectedThing;
            if (selectedThing == null)
            {
                _lastSelectedThingId = null;
                return;
            }

            if (_lastSelectedThingId != null && selectedThing.ThingID == _lastSelectedThingId)
                return;

            _lastSelectedThingId = selectedThing.ThingID;
            if (__instance.OpenTabType != null && __instance.OpenTabType == tab.GetType())
                return;

            tab.OnOpen();
            __instance.OpenTabType = tab.GetType();
        }

        private static string _lastSelectedThingId;
    }
}