using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;

namespace ImprovedWorkbenches
{
    [StaticConstructorOnStartup]
    public class Dialog_ThingFilter : Dialog_MessageBox
    {
        public ThingFilter filter;

        public Vector2 scrollPosition;

        public Window reOpenWindow;

        public Dialog_ThingFilter(ExtendedBillData extendedBill, Window w) : base("Do Until X also includes these:")
        {
            reOpenWindow = w;
            filter = new ThingFilter();
            if (extendedBill.ProductAdditionalFilter != null)
                filter.CopyAllowancesFrom(extendedBill.ProductAdditionalFilter);

            buttonAAction = () =>
            {
                if (extendedBill.ProductAdditionalFilter == null)
                    extendedBill.ProductAdditionalFilter = filter;
                else
                    extendedBill.ProductAdditionalFilter.CopyAllowancesFrom(filter);
            };

            buttonBText = "Stop Using Filter";
            buttonBAction = () =>
            {
                extendedBill.ProductAdditionalFilter = null;
            };
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
            base.DoWindowContents(inRect);

            Rect filterRect = inRect.BottomPartPixels(inRect.height - 40);
            filterRect.height -= 40;
            ThingFilterUI.DoThingFilterConfigWindow(filterRect, ref scrollPosition, filter,
                openMask: TreeOpenMasks.ThingFilter,
                parentFilter: baseFilter);
        }

        public override void PreClose()
        {
            base.PreClose();
            Find.WindowStack.Add(reOpenWindow);
        }
    }
}
