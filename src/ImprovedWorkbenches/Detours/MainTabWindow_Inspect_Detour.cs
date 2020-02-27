using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    // Automatically open main tab when a workbench or stockpile is selected

    [HarmonyPatch(typeof(MainTabWindow_Inspect), "ExtraOnGUI")]
    public static class MainTabWindow_Inspect_Detour
    {
        [HarmonyPostfix]
        public static void Postfix(MainTabWindow_Inspect __instance)
        {
            if (!Main.Instance.ShouldExpandBillsTab())
                return;

            var tab = __instance.CurTabs?.FirstOrDefault();
            if (tab == null)
                return;

            var tabIsBills = false;

            if (tab is ITab_Bills || Main.Instance.IsOfTypeRimFactoryBillsTab(tab))
            {
                tabIsBills = true;
            }
            else if (!(tab is ITab_Storage))
            {
                // No workbench or stockpile selected
                _lastSelectedThingId = null;
                return;
            }

            // We must make sure we only open the tab once, or we'll keep reopening
            // it even if the player wants to manually close it.
            string selectedThingId = null;
            if (tabIsBills)
            {
                // Workbench
                var selectedThing = Find.Selector.SingleSelectedThing;
                if (selectedThing != null)
                {
                    selectedThingId = selectedThing.ThingID;
                }
            }
            else
            {
                // Stockpile
                var selectedZone = Find.Selector.SelectedZone;
                if (selectedZone != null)
                {
                    selectedThingId = selectedZone.GetHashCode().ToString();
                }
            }

            if (selectedThingId == null)
            {
                _lastSelectedThingId = null;
                return;
            }

            if (_lastSelectedThingId != null && selectedThingId == _lastSelectedThingId)
                return;

            _lastSelectedThingId = selectedThingId;
            if (__instance.OpenTabType != null && __instance.OpenTabType == tab.GetType())
                return;

            tab.OnOpen();
            __instance.OpenTabType = tab.GetType();
        }

        private static string _lastSelectedThingId;
    }
}