using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    public class ExtendedBillData : IExposable
    {
        public ThingFilter OutputFilter = new ThingFilter();
        public bool AllowDeadmansApparel;
        public bool UseInputFilter;
        public Pawn Worker;

        public ExtendedBillData()
        {
        }

        // Constructor for migrating old data storage format to new method.
        public ExtendedBillData(Bill_Production bill)
        {
            var billWithWorkerFilter = bill as IBillWithWorkerFilter;
            Worker = billWithWorkerFilter.GetWorker();

            if (!BillUtility_Detour.CanOutputBeFiltered(bill))
                return;

            var billWithThingFilter = bill as IBillWithThingFilter;
            if (billWithThingFilter == null)
                return;

            OutputFilter = billWithThingFilter.GetOutputFilter();
            AllowDeadmansApparel = billWithThingFilter.GetAllowDeadmansApparel();
            UseInputFilter = billWithThingFilter.GetUseInputFilter();
        }

        public void ExposeData()
        {
            Scribe_Deep.Look(ref OutputFilter, "outputFilter", new object[0]);
            Scribe_Values.Look(ref AllowDeadmansApparel,
                "allowDeadmansApparel", false);
            Scribe_Values.Look(ref UseInputFilter,
                "useInputFilter", false);
            Scribe_References.Look(ref Worker, "worker");
        }
    }
}