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
        private ExtendedBillData _extendedBill;

        public ThingFilter filter;

        public Vector2 scrollPosition;

        public Dialog_ThingFilter(ExtendedBillData extendedBill) : base("Product Filter")
        {
            _extendedBill = extendedBill;

            filter = new ThingFilter();
            if (_extendedBill.ProductAdditionalFilter != null)
                filter.CopyAllowancesFrom(_extendedBill.ProductAdditionalFilter);

            buttonAAction = () =>
            {
                Log.Message("APPLIED");
                if (_extendedBill.ProductAdditionalFilter == null)
                    _extendedBill.ProductAdditionalFilter = filter;
                else
                    _extendedBill.ProductAdditionalFilter.CopyAllowancesFrom(filter);
            };

            buttonBText = "Default Filter";
            buttonBAction = () =>
            {
                Log.Message("Clear");
                _extendedBill.ProductAdditionalFilter = null;
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
