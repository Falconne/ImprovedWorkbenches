using Verse;

namespace ImprovedWorkbenches
{
    public class Dialog_RenameBill : Dialog_Rename
    {
        private readonly ExtendedBillData _extendedBill;

        private readonly string _defaultName;

        public Dialog_RenameBill(ExtendedBillData extendedBill, string defaultName)
        {
            _extendedBill = extendedBill;
            _defaultName = defaultName;
            curName = string.IsNullOrEmpty(extendedBill.Name) ? defaultName : extendedBill.Name;
        }

        protected override void SetName(string name)
        {
            if (string.IsNullOrEmpty(name) || name != _defaultName)
                _extendedBill.Name = name;
        }

        protected override AcceptanceReport NameIsValid(string name)
        {
            return true;
        }
    }
}