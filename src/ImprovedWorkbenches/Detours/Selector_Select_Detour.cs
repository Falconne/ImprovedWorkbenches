using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace ImprovedWorkbenches
{
    // Automatically open proper tab when a workbench or stockpile is selected
    [HarmonyPatch(typeof(Selector), "Select")]
    public static class Open_Bills_Tab_On_Select
    {
        public static void Postfix(Selector __instance)
        {
            if (!Main.Instance.ShouldExpandBillsTab())
                return;

            try
            {
                // Has one single thing been selected:
                if (__instance.NumSelected != 1)
                    return;

                var tabsOfSelectedThing = GetTabsOfAnySelectedThingWeCareAbout(__instance)?.ToList();
                if (tabsOfSelectedThing == null)
                    return;

                // If any tab of selected thing is currently open, don't override it
                var pane = (MainTabWindow_Inspect) MainButtonDefOf.Inspect.TabWindow;
                Type alreadyOpenTabType = pane.OpenTabType;
                if (alreadyOpenTabType != null)
                {
                    foreach (var tab in tabsOfSelectedThing)
                    {
                        // This misses any subclassing, but that's probably okay?
                        // If it's not, add in some .isSubClassOf()s
                        if (tab.GetType() == alreadyOpenTabType)
                        {
                            // Leave selected tab alone
                            return;
                        }
                    }
                }

                // Select any storage or bill tab the selected thing might have
                //var tabToSelect = listOfTabs.OfType<ITab_Storage>().FirstOrDefault();
                var tabToSelect = tabsOfSelectedThing
                    .FirstOrDefault(t => t is ITab_Storage || t is ITab_Bills);

                if (tabToSelect == null)
                    return;

                // These two things are what makes a tab open:
                tabToSelect.OnOpen();
                pane.OpenTabType = tabToSelect.GetType();
            }
            catch (Exception e)
            {
                Main.Instance.Logger.Error(
                    "Error trying to select bill or storage tab. Please report this to the Better Workbench Management mod page");
                Main.Instance.Logger.Error(e.Message);
                Main.Instance.Logger.Error(e.StackTrace);
            }
        }

        private static IEnumerable<InspectTabBase> GetTabsOfAnySelectedThingWeCareAbout(Selector __instance)
        {
            if (__instance.SelectedZone is Zone_Stockpile stockpile)
                return stockpile.GetInspectTabs();

            if (__instance.SingleSelectedThing is Building_WorkTable workTable)
                return workTable.GetInspectTabs();

            if (__instance.SingleSelectedThing is Building_Storage storage)
            {
                if (ModLister.HasActiveModWithName("LWM's Deep Storage"))
                {
                    // Deep Storage handles tabs on its own for storage buildings
                    return null;
                }

                return storage.GetInspectTabs();
            }

            return null;
        }
    }

}
