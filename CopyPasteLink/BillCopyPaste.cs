using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    public class BillCopyPaste
    {
        private readonly List<Bill_Production> _copiedBills = new List<Bill_Production>();

        private ThingFilter _copiedFilter;

        private ThingFilter _copiedFiltersParent;

        public void Clear()
        {
            _copiedBills.Clear();
        }

        public void RemoveBill(Bill_Production bill)
        {
            _copiedBills.Remove(bill);
        }

        public void DoCopy(Bill_Production bill)
        {
            _copiedBills.Clear();
            _copiedBills.Add(bill);
        }

        public void DoCopy(Building_WorkTable workTable)
        {
            if (workTable.BillStack == null || workTable.BillStack.Count == 0)
                return;

            _copiedBills.Clear();
            foreach (var bill in workTable.BillStack.Bills)
            {
                var billProduction = bill as Bill_Production;
                if (billProduction == null)
                    continue;

                _copiedBills.Add(billProduction);
            }
        }

        public bool CanPasteInto(Building_WorkTable workTable)
        {
            if (_copiedBills.Count == 0)
                return false;

            if (workTable.BillStack == null || workTable.BillStack.Count >= 15)
                return false;

            _copiedBills.RemoveAll(bill => bill.DeletedOrDereferenced);

            foreach (var bill in _copiedBills)
            {
                if (CanWorkTableDoRecipeNow(workTable, bill.recipe))
                    return true;
            }

            return false;
        }

        public bool IsMultipleBillsCopied()
        {
            return _copiedBills.Count > 1;
        }

        public void DoPasteInto(Building_WorkTable workTable, bool link)
        {
            foreach (var sourceBill in _copiedBills)
            {
                if (sourceBill.DeletedOrDereferenced)
                    continue;

                if (!CanWorkTableDoRecipeNow(workTable, sourceBill.recipe))
                    continue;

                var newBill = (Bill_Production)sourceBill.recipe.MakeNewBill();
                workTable.BillStack.AddBill(newBill);

                Main.Instance.GetExtendedBillDataStorage().MirrorBills(sourceBill, newBill);

                if (link)
                {
                    Main.Instance.GetExtendedBillDataStorage().LinkBills(sourceBill, newBill);
                }
            }
        }

        public bool IsMatchingFilterCopied(ThingFilter parent)
        {
            return _copiedFilter != null && DoFiltersMatch(_copiedFiltersParent, parent);
        }

        public void CopyFilter(ThingFilter filter, ThingFilter parent)
        {
            _copiedFilter = new ThingFilter();
            _copiedFilter.CopyAllowancesFrom(filter);
            _copiedFiltersParent = parent;
        }

        public void PasteCopiedFilterInto(ThingFilter other)
        {
            other.CopyAllowancesFrom(_copiedFilter);
        }

        private static bool CanWorkTableDoRecipeNow(Building_WorkTable workTable, RecipeDef recipe)
        {
            return workTable.BillStack.Count < 15 &&
                recipe.AvailableNow &&
                workTable.def.AllRecipes != null &&
                workTable.def.AllRecipes.Contains(recipe);
        }

        private bool DoFiltersMatch(ThingFilter first, ThingFilter second)
        {
            if (first == null && second == null)
                return true;

            if (first == null || second == null)
                return false;

            // Only matching on allowed things for performance. May need to match
            // other fields in the future;
            if (first.AllowedDefCount != second.AllowedDefCount)
                return false;

            foreach (var thingDef in first.AllowedThingDefs)
            {
                if (first.Allows(thingDef) != second.Allows(thingDef))
                {
                    return false;
                }
            }

            return true;
        }
    }
}