using System;
using System.Linq;
using Harmony;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    // Automatically open proper tab when a workbench or stockpile is selected
    [HarmonyPatch(typeof(Selector), "Select")]
    public static class Open_Bills_Tab_On_Select {
        public static void Postfix(Selector __instance) {
            // Has one single thing been selected:
            if (__instance.NumSelected != 1) return;
            Zone z = __instance.SelectedZone;
            Thing t = __instance.SingleSelectedThing;
            // Do we have either a stockpile or a worktable or a storage building?
            // If not, don't try to open the wrong tab:
            if ((z!=null && !(z is Zone_Stockpile)) || // is it a non Stockpile zone?
                (z == null && (t == null // not a zone nor a building
                                  // it's a thing, but not a building we care about
                               || !(t is Building_WorkTable || t is Building_Storage)
                               )
                 )) return;
            // I'm writing this, so I get to build in compatibility for myself!
            if ((t as Building_Storage != null) && ModLister.HasActiveModWithName("LWM's Deep Storage")) {
                return;  // Deep Storage handles tabs on its own for storage buildings
            }
            // Fill tab with the proper tab we want open for the new selection
            ITab tab=null;
            // But check to see if a tab is already open - we don't want
            //   to override an open tab if the same tab can be open for
            //   the new selection
            var pane = (MainTabWindow_Inspect)MainButtonDefOf.Inspect.TabWindow;
            Type alreadyOpenTabType = pane.OpenTabType;
            if (alreadyOpenTabType != null) {
                // this is the list of all the tabs the newly selected thing CAN have
                System.Collections.Generic.IEnumerable<InspectTabBase> listOfTabs;
                if (z != null)
                    listOfTabs = z.GetInspectTabs();
                else
                    listOfTabs = t.GetInspectTabs();
                foreach (var x in listOfTabs) {
                    // This misses any subclassing, but that's probably okay?
                    // If it's not, add in some .isSubClassOf()s
                    if (x.GetType() == alreadyOpenTabType) {
                        return; // standard Selector behavior should kick in.
                    }
                    // If it has Storage or Bills tab, go ahead and select it!
                    if (x.GetType() == typeof(ITab_Storage)) tab = (ITab)x;
                    if (x.GetType() == typeof(ITab_Bills)) tab = (ITab)x;
                }
            } else { // We still need to grab the Storage or Bills tab:
                // If it's a zone, open Storage!
                if (z != null) {
                    tab = z.GetInspectTabs().OfType<ITab_Storage>().First();
                } else if (t is Building_Storage) { // same for storage buildings
                    tab = t.GetInspectTabs().OfType<ITab_Storage>().First();
                } else { // workbench: open Bills!
                    tab = t.GetInspectTabs().OfType<ITab_Bills>().First();
                }
            }
            if (tab==null) {
                Log.Warning("" + (z != null ? ("Zone " + z) : ("Building " + t)) + " does not have a Storage or Bills tab?");
                return;
            }
            // These two things are what makes a tab open:
            tab.OnOpen();
            pane.OpenTabType = tab.GetType();
        }
    } // end patch of Select to open ITab
}
