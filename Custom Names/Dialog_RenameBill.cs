using Verse;

namespace ImprovedWorkbenches
{
    public class Dialog_RenameBill : Dialog_Rename
    {
        private readonly ExtendedBillData _extendedBill;

        public Dialog_RenameBill(ExtendedBillData extendedBill)
        {
            _extendedBill = extendedBill;
            curName = extendedBill.Name ?? "";
        }

        protected override void SetName(string name)
        {
            _extendedBill.Name = name;
        }

        protected override AcceptanceReport NameIsValid(string name)
        {
            return true;
        }
    }
}