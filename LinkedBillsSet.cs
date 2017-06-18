using System.Collections.Generic;
using Verse;

namespace ImprovedWorkbenches
{
    public class LinkedBillsSet : IExposable
    {
        public List<int> BillIds = new List<int>();

        public void ExposeData()
        {
            Scribe_Collections.Look(ref BillIds, "BillIds", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.LoadingVars && BillIds == null)
            {
                BillIds = new List<int>();
            }
        }
    }
}