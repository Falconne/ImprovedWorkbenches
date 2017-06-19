using System.Linq;
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
        public string Name;

        public ExtendedBillData()
        {
        }

        // Constructor for migrating old data storage format to new method.
        public ExtendedBillData(Bill_Production bill)
        {
            var billWithWorkerFilter = bill as IBillWithWorkerFilter;
            Worker = billWithWorkerFilter.GetWorker();

            if (!ExtendedBillDataStorage.CanOutputBeFiltered(bill))
                return;

            var billWithThingFilter = bill as IBillWithThingFilter;
            if (billWithThingFilter == null)
                return;

            OutputFilter = billWithThingFilter.GetOutputFilter();
            AllowDeadmansApparel = billWithThingFilter.GetAllowDeadmansApparel();
            UseInputFilter = billWithThingFilter.GetUseInputFilter();
        }

        public void CloneFrom(ExtendedBillData other)
        {
            OutputFilter.CopyAllowancesFrom(other.OutputFilter);
            AllowDeadmansApparel = other.AllowDeadmansApparel;
            UseInputFilter = other.UseInputFilter;
            Worker = other.Worker;
            Name = other.Name;
        }

        public void SetDefaultFilter(Bill_Production bill)
        {
            var thingDef = bill.recipe.products.First().thingDef;
            OutputFilter.SetDisallowAll();
            OutputFilter.SetAllow(thingDef, true);
        }

        public void ExposeData()
        {
            Scribe_Deep.Look(ref OutputFilter, "outputFilter", new object[0]);
            Scribe_Values.Look(ref AllowDeadmansApparel, "allowDeadmansApparel", false);
            Scribe_Values.Look(ref UseInputFilter, "useInputFilter", false);
            Scribe_References.Look(ref Worker, "worker");
            Scribe_Values.Look(ref Name, "name", null);
        }
    }
}