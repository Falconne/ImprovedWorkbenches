using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    public class BillCopyPaste
    {
        private readonly List<Bill_Production> _copiedBills = new List<Bill_Production>();

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

        public void DoCopy(BillStack bills)
        {
            if (bills == null || bills.Count == 0)
                return;

            _copiedBills.Clear();
            foreach (var bill in bills.Bills)
            {
                var billProduction = bill as Bill_Production;
                if (billProduction == null)
                    continue;

                _copiedBills.Add(billProduction);
            }
        }

        public bool CanPasteInto(BillStack bills, IEnumerable<RecipeDef> recipes)
        {
            if (_copiedBills.Count == 0)
                return false;

            if (bills == null)
                return false;

            _copiedBills.RemoveAll(bill => bill == null || bill.DeletedOrDereferenced);

            foreach (var bill in _copiedBills)
            {
                if (!CanWorkTableDoRecipeNow(bills, recipes, bill.recipe))
                {
                    return false;
                }
            }

            return true;
        }

        public bool CanPasteInto(Bill_Production targetBill)
        {
            return _copiedBills.Count == 1 && _copiedBills.First() != targetBill;
        }

        public void DoPasteInto(BillStack bills, IEnumerable<RecipeDef> recipes, bool link)
        {
            foreach (var sourceBill in _copiedBills)
            {
                if (sourceBill.DeletedOrDereferenced)
                    continue;

                if (!CanWorkTableDoRecipeNow(bills, recipes, sourceBill.recipe))
                    continue;

                var newBill = (Bill_Production)sourceBill.recipe.MakeNewBill();
                bills.AddBill(newBill);

                Main.Instance.GetExtendedBillDataStorage().MirrorBills(sourceBill, newBill, false);

                if (link)
                {
                    Main.Instance.GetExtendedBillDataStorage().LinkBills(sourceBill, newBill);
                }
            }
        }

        public void DoPasteInto(Bill_Production targetBill)
        {
            var sourceBill = _copiedBills.FirstOrDefault();
            if (sourceBill == null || sourceBill.DeletedOrDereferenced)
                return;

            Main.Instance.GetExtendedBillDataStorage().MirrorBills(sourceBill, targetBill, true);
        }

        private static bool CanWorkTableDoRecipeNow(BillStack bills, IEnumerable<RecipeDef> recipes, RecipeDef recipe)
        {
            return bills.Count < Main.Instance.GetMaxBills() &&
                recipe.AvailableNow &&
                recipes != null &&
                recipes.Contains(recipe);
        }
    }
}