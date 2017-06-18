using System.Collections.Generic;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    public class LinkedBillsSet : IExposable
    {
        public int Count => _bills.Count;

        private List<Bill_Production> _bills = new List<Bill_Production>();

        public void ExposeData()
        {
            Scribe_Collections.Look(ref _bills, "Bills", LookMode.Reference);

            if (Scribe.mode == LoadSaveMode.LoadingVars && _bills == null)
            {
                _bills = new List<Bill_Production>();
            }
        }

        public void Add(Bill_Production bill)
        {
            _bills.Add(bill);
        }

        public void Remove(Bill_Production bill)
        {
            _bills.Remove(bill);
        }

        public bool Contains(Bill_Production bill)
        {
            return _bills.Contains(bill);
        }
    }
}