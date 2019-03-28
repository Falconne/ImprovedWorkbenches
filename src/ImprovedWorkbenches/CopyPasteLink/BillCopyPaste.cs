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

            if (workTable.BillStack == null)
                return false;

            _copiedBills.RemoveAll(bill => bill == null || bill.DeletedOrDereferenced);

            foreach (var bill in _copiedBills)
            {
                if (!CanWorkTableDoRecipeNow(workTable, bill.recipe))
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


        private static bool CanWorkTableDoRecipeNow(Building_WorkTable workTable, RecipeDef recipe)
        {
            return workTable.BillStack.Count < 15 &&
                recipe.AvailableNow &&
                workTable.def.AllRecipes != null &&
                workTable.def.AllRecipes.Contains(recipe);
        }
    }
}