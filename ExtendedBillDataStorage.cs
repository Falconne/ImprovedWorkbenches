using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HugsLib.Utils;
using ImprovedWorkbenches;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    public class ExtendedBillDataStorage : UtilityWorldObject, IExposable
    {
        private Dictionary<int, ExtendedBillData> _store =
            new Dictionary<int, ExtendedBillData>();

        private List<LinkedBillsSet> _linkedBillsSets = new List<LinkedBillsSet>();

        private List<int> _billIDsWorkingList;

        private List<ExtendedBillData> _extendedBillDataWorkingList;

        private static readonly FieldInfo LoadIdGetter = typeof(Bill).GetField("loadID",
            BindingFlags.NonPublic | BindingFlags.Instance);


        public override void ExposeData()
        {
            base.ExposeData();
			Scribe_Collections.Look(
                ref _store, "store", 
                LookMode.Value, LookMode.Deep, 
                ref _billIDsWorkingList, ref _extendedBillDataWorkingList);

            Scribe_Collections.Look(ref _linkedBillsSets, "linkedBillsSets", 
                LookMode.Deep);

            if (Scribe.mode == LoadSaveMode.LoadingVars && _linkedBillsSets == null)
            {
                _linkedBillsSets = new List<LinkedBillsSet>();
            }
        }

        // Return the associate extended data for a given bill, creating a new association
        // if required.
        public ExtendedBillData GetExtendedDataFor(Bill_Production bill)
        {

            var loadId = GetBillId(bill);
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
            }
            else
            {
                newExtendedData = new ExtendedBillData();
                if (CanOutputBeFiltered(bill))
                    newExtendedData.SetDefaultFilter(bill);
            }

            _store[loadId] = newExtendedData;
            return newExtendedData;
        }

        // Delete extended data when bill is deleted
        public void DeleteExtendedDataFor(Bill_Production bill)
        {
            var billId = GetBillId(bill);
            RemoveBillFromLinkSets(bill);
            _store.Remove(billId);
        }

        public void LinkBills(Bill_Production parent, Bill_Production child)
        {
            var parentId = GetBillId(parent);
            var childId = GetBillId(child);
            Main.Instance.Logger.Message($"Linking bills {parentId} -> {childId}");

            var existingBillSet = GetBillSetContaining(parent);
            if (existingBillSet != null)
            {
                Main.Instance.Logger.Message($"Existing set found with {existingBillSet.Count} entries");
                existingBillSet.Add(child);
                return;
            }

            Main.Instance.Logger.Message("Creating new set");
            var newSet = new LinkedBillsSet();
            newSet.Add(parent);
            newSet.Add(child);
            _linkedBillsSets.Add(newSet);
        }

        public LinkedBillsSet GetBillSetContaining(Bill_Production bill)
        {
            foreach (var billsSet in _linkedBillsSets)
            {
                if (billsSet.Contains(bill))
                    return billsSet;
            }

            return null;
        }

        public void RemoveBillFromLinkSets(Bill_Production bill)
        {
            var existingBillSet = GetBillSetContaining(bill);
            if (existingBillSet == null)
                return;

            Main.Instance.Logger.Message("Removing bill from existing set");
            if (existingBillSet.Count <= 2)
            {
                Main.Instance.Logger.Message("Removing entire set");
                _linkedBillsSets.Remove(existingBillSet);
            }
            else
            {
                existingBillSet.Remove(bill);
            }

            Main.Instance.Logger.Message($"Link sets remaining: {_linkedBillsSets.Count}");
        }

        // Figure out if output of bill produces a "thing" with quality or hit-points
        public static bool CanOutputBeFiltered(Bill_Production bill)
        {
            return CanOutputBeFiltered(bill.recipe);
        }

        // Figure out if output of recipe produces a "thing" with quality or hit-points
        private static bool CanOutputBeFiltered(RecipeDef recipe)
        {
            if (recipe.products == null || recipe.products.Count == 0)
                return false;

            var thingDef = recipe.products.First().thingDef;
            if (thingDef.BaseMarketValue <= 0)
                return false;

            return !thingDef.CountAsResource;
        }

        private int GetBillId(Bill_Production bill)
        {
            return (int)LoadIdGetter.GetValue(bill);
        }
    }
}