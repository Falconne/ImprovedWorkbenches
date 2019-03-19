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

        public Dialog_ThingFilter(ExtendedBillData extendedBill, Window w) : base("Product Filter")
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

            buttonBText = "Default Filter";
            buttonBAction = () =>
            {
                extendedBill.ProductAdditionalFilter = null;
            };
        }

        List<SpecialThingFilterDef> specialThingDefs = DefDatabase<SpecialThingFilterDef>.AllDefs.ToList();
        public override void DoWindowContents(Rect inRect)
        {
            base.DoWindowContents(inRect);

            Rect filterRect = new Rect(inRect);
            filterRect.height -= 40;
            ThingFilterUI.DoThingFilterConfigWindow(filterRect, ref scrollPosition, filter, openMask: TreeOpenMasks.ThingFilter, forceHideHitPointsConfig: true, forceHiddenFilters: specialThingDefs);
        }

        public override void PreClose()
        {
            base.PreClose();
            Find.WindowStack.Add(reOpenWindow);
        }
    }
}
