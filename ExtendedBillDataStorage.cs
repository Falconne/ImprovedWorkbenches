using System.Collections.Generic;
using ImprovedWorkbenches;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    public class ExtendedBillDataStorage : IExposable
    {
        private Dictionary<int, ExtendedBillData> store =
            new Dictionary<int, ExtendedBillData>();

        private List<int> _billIDsWorkingList;

        private List<ExtendedBillData> _extendedBillDataWorkingList;

        public void ExposeData()
        {
			Scribe_Collections.Look(
                ref store, "store", 
                LookMode.Value, LookMode.Deep, 
                ref _billIDsWorkingList, ref _extendedBillDataWorkingList);
        }
    }
}