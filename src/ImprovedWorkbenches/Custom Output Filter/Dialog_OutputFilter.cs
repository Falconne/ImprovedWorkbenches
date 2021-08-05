using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;

namespace ImprovedWorkbenches
{
    [StaticConstructorOnStartup]
    public class Dialog_ThingFilter : Window
    {
        public ThingFilter filter;
        public ExtendedBillData extendedBill;

        public ThingFilterUI.UIState uiState;

        public Window reOpenWindow;

        public Dialog_ThingFilter(ExtendedBillData e, Window w)
        {
            reOpenWindow = w;
            extendedBill = e;

            filter = new ThingFilter();
            if (extendedBill.ProductAdditionalFilter != null)
                filter.CopyAllowancesFrom(extendedBill.ProductAdditionalFilter);
        }

        //to hide HP, quality, special filters, this is needed:
        static ThingFilter baseFilter;
        static Dialog_ThingFilter()
        {
            baseFilter = new ThingFilter();
            baseFilter.SetAllowAll(null);
            baseFilter.DisplayRootCategory = ThingCategoryNodeDatabase.RootNode;
            baseFilter.allowedHitPointsConfigurable = false;
            baseFilter.allowedQualitiesConfigurable = false;
        }
        static List<SpecialThingFilterDef> specialThingDefs = DefDatabase<SpecialThingFilterDef>.AllDefs.ToList();
        public override void DoWindowContents(Rect inRect)
        {
            Rect titleRect = inRect.TopPartPixels(40);
            Rect buttonsRect = inRect.BottomPartPixels(40);
            Rect okayRect = buttonsRect.RightHalf();
            Rect filterRect = inRect;
            filterRect.height -= 80;
            filterRect.y += 35;

            Widgets.Label(titleRect, "IW.OutputFilterTitle".Translate());

            ThingFilterUI.DoThingFilterConfigWindow(filterRect, uiState, filter,
                openMask: TreeOpenMasks.ThingFilter,
                forceHiddenFilters: specialThingDefs,
                parentFilter: baseFilter);

            if (Widgets.ButtonText(okayRect, "OK".Translate()))
            {
                if (filter.AllowedThingDefs.Count() == 0)
                    extendedBill.ProductAdditionalFilter = null;
                else
                {
                    if (extendedBill.ProductAdditionalFilter == null)
                        extendedBill.ProductAdditionalFilter = filter;
                    else
                        extendedBill.ProductAdditionalFilter.CopyAllowancesFrom(filter);
                }
                Close();
            }
        }

        public override void PreClose()
        {
            base.PreClose();
            Find.WindowStack.Add(reOpenWindow);
        }
    }
}
