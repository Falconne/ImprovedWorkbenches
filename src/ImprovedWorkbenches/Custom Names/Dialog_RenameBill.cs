using Verse;

namespace ImprovedWorkbenches
{
    public class Dialog_RenameBill : Dialog_Rename<IRenameable>
    {
        private readonly ExtendedBillData _extendedBill;


        public Dialog_RenameBill(ExtendedBillData extendedBill) : base(null)
        {
             _extendedBill = extendedBill;
        }

        protected override void OnRenamed(string name)
        {
            _extendedBill.Name = name;
        }
    }
}