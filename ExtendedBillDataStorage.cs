using System.Collections.Generic;
using System.Reflection;
using ImprovedWorkbenches;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    public class ExtendedBillDataStorage : IExposable
    {
        private Dictionary<int, ExtendedBillData> _store =
            new Dictionary<int, ExtendedBillData>();

        private List<int> _billIDsWorkingList;

        private List<ExtendedBillData> _extendedBillDataWorkingList;

        private static readonly FieldInfo LoadIdGetter = typeof(Bill).GetField("loadID",
            BindingFlags.NonPublic | BindingFlags.Instance);

        public void ExposeData()
        {
			Scribe_Collections.Look(
                ref _store, "store", 
                LookMode.Value, LookMode.Deep, 
                ref _billIDsWorkingList, ref _extendedBillDataWorkingList);
        }

        public ExtendedBillData GetDataFor(Bill_Production bill)
        {

            var loadId = (int) LoadIdGetter.GetValue(bill);
            if (_store.TryGetValue(loadId, out ExtendedBillData data))
            {
                return data;
            }

            ExtendedBillData newExtendedData;
            if (bill is IBillWithThingFilter)
            {
                Main.Instance.Logger.Warning(
                    $"Found old Bill ({bill.GetUniqueLoadID()}), migrating to new format");

                newExtendedData = new ExtendedBillData(bill);
                _store[loadId] = newExtendedData;
            }
            else
            {
                Main.Instance.Logger.Message(
                    $"Creating new data for {bill.GetUniqueLoadID()}");
                newExtendedData = new ExtendedBillData();
            }

            return newExtendedData;
        }
    }
}