using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    public class ExtendedBillData : IExposable
    {
        public bool CountAway;
        public string Name;

        public ExtendedBillData()
        {
        }

        public void CloneFrom(ExtendedBillData other, bool cloneName)
        {
            CountAway = other.CountAway;
            if (cloneName)
                Name = other.Name;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref CountAway, "countAway", false);
            Scribe_Values.Look(ref Name, "name", null);
        }
    }
}