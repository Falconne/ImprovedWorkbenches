using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    public class LinkedBillsSet : IExposable
    {
        public IList<Bill_Production> Bills => _bills;

        private List<Bill_Production> _bills = new List<Bill_Production>();

        public void ExposeData()
        {
            Scribe_Collections.Look(ref _bills, "IW.BillsLabel".Translate(), LookMode.Reference);

            if (Scribe.mode == LoadSaveMode.LoadingVars && _bills == null)
            {
                _bills = new List<Bill_Production>();
            }
        }
    }
}