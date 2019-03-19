using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using UnityEngine;

namespace ImprovedWorkbenches
{
    public class Dialog_ThingFilter : Dialog_MessageBox
    {
        public ThingFilter filter;

        public Vector2 scrollPosition;

        public Dialog_ThingFilter(ExtendedBillData extendedBill) : base("Product Filter")
        {
            filter = new ThingFilter();
            if (extendedBill.ProductAdditionalFilter != null)
                filter.CopyAllowancesFrom(extendedBill.ProductAdditionalFilter);

            buttonAAction = () =>
            {
                Log.Message("APPLIED");
                if (extendedBill.ProductAdditionalFilter == null)
                    extendedBill.ProductAdditionalFilter = filter;
                else
                    extendedBill.ProductAdditionalFilter.CopyAllowancesFrom(filter);
            };

            buttonBText = "Default Filter";
            buttonBAction = () =>
            {
                Log.Message("Clear");
                extendedBill.ProductAdditionalFilter = null;
            };
        }


        public override void DoWindowContents(Rect inRect)
        {
            base.DoWindowContents(inRect);

            Rect filterRect = new Rect(inRect);
            filterRect.height -= 40;
            ThingFilterUI.DoThingFilterConfigWindow(filterRect, ref scrollPosition, filter);
        }
    }
}
