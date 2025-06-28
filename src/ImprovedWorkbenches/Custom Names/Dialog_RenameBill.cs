using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    public class Dialog_RenameBill : Dialog_Rename<IRenameable>
    {
        private readonly ExtendedBillData _extendedBill;


        public Dialog_RenameBill(ExtendedBillData extendedBill, Bill_Production bill) : base(null)
        {
             _extendedBill = extendedBill;

            if (string.IsNullOrEmpty(_extendedBill.Name))
            {
                curName = bill.LabelCap;
            }
            else
            {
                curName = _extendedBill.Name;
            }
        }

        protected override void OnRenamed(string name)
        {
            _extendedBill.Name = name;
        }

        protected override AcceptanceReport NameIsValid(string name)
        {
            return true;
        }
    }
}