using Verse;

namespace ImprovedWorkbenches
{
    public class Dialog_RenameBill : Dialog_Rename
    {
        private readonly ExtendedBillData _extendedBill;

        private readonly string _currentName;

        public Dialog_RenameBill(ExtendedBillData extendedBill, string currentName)
        {
            _extendedBill = extendedBill;
            _currentName = currentName;
            curName = currentName;
        }

        public override void SetName(string name)
        {
            if (string.IsNullOrEmpty(name) || name != _currentName)
                _extendedBill.Name = name;
        }

        public override AcceptanceReport NameIsValid(string name)
        {
            return true;
        }
    }
}