using Verse;

namespace ImprovedWorkbenches
{
    public class ExtendedBillData : IExposable
    {
        public ThingFilter OutputFilter = new ThingFilter();
        public bool AllowDeadmansApparel;
        public bool UseInputFilter;
        public Pawn Worker;

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