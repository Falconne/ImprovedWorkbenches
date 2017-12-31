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
                    "IW.FoundOldBillText".Translate() + " " + $"({bill.GetUniqueLoadID()})" + ", " + "IW.MigratingToNewFormat".Translate());

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
            var existingBillSet = GetBillSetContaining(parent);
            if (existingBillSet != null)
            {
                existingBillSet.Bills.Add(child);
                return;
            }

            var newSet = new LinkedBillsSet();
            newSet.Bills.Add(parent);
            newSet.Bills.Add(child);
            _linkedBillsSets.Add(newSet);
        }

        public LinkedBillsSet GetBillSetContaining(Bill_Production bill)
        {
            foreach (var billsSet in _linkedBillsSets)
            {
                if (billsSet.Bills.Contains(bill))
                    return billsSet;
            }

            return null;
        }

        public bool IsLinkedBill(Bill_Production bill)
        {
            return GetBillSetContaining(bill) != null;
        }

        public void RemoveBillFromLinkSets(Bill_Production bill)
        {
            var existingBillSet = GetBillSetContaining(bill);
            if (existingBillSet == null)
                return;

            if (existingBillSet.Bills.Count <= 2)
            {
                _linkedBillsSets.Remove(existingBillSet);
            }
            else
            {
                existingBillSet.Bills.Remove(bill);
            }
        }

        public void MirrorBillToLinkedBills(Bill_Production sourceBill)
        {
            var existingBillSet = GetBillSetContaining(sourceBill);
            if (existingBillSet == null)
                return;

            foreach (var linkedBill in existingBillSet.Bills)
            {
                if (linkedBill == sourceBill)
                    continue;

                MirrorBills(sourceBill, linkedBill);
            }
        }

        public void MirrorBills(Bill_Production sourceBill, Bill_Production destinationBill)
        {
            destinationBill.ingredientFilter?.CopyAllowancesFrom(sourceBill.ingredientFilter);
            destinationBill.ingredientSearchRadius = sourceBill.ingredientSearchRadius;
            destinationBill.allowedSkillRange = sourceBill.allowedSkillRange;
            destinationBill.repeatMode = sourceBill.repeatMode;
            destinationBill.repeatCount = sourceBill.repeatCount;
            destinationBill.targetCount = sourceBill.targetCount;
            destinationBill.storeMode = sourceBill.storeMode;
            destinationBill.pauseWhenSatisfied = sourceBill.pauseWhenSatisfied;
            destinationBill.unpauseWhenYouHave = sourceBill.unpauseWhenYouHave;
            destinationBill.paused = sourceBill.paused;

            var sourceExtendedData = GetExtendedDataFor(sourceBill);

            if (sourceExtendedData == null)
                return;

            var destinationExtendedData = GetExtendedDataFor(destinationBill);

            destinationExtendedData?.CloneFrom(sourceExtendedData);

        }

        public void OnStockpileDeteled(Zone_Stockpile stockpile)
        {
            foreach (var extendedBillData in _store.Values)
            {
                if (extendedBillData.UsesCountingStockpile() 
                    && extendedBillData.GetCountingStockpile() == stockpile)
                {
                    extendedBillData.RemoveCountingStockpile();
                }

                if (extendedBillData.UsesTakeToStockpile()
                    && extendedBillData.GetTakeToStockpile() == stockpile)
                {
                    extendedBillData.RemoveTakeToStockpile();
                }
            }
        }

        // Figure out if output of bill produces a "thing" we care about
        public static bool CanOutputBeFiltered(Bill_Production bill)
        {
            return CanOutputBeFiltered(bill.recipe);
        }

        
        // Figure out if output of recipe produces a "thing" we care about
        private static bool CanOutputBeFiltered(RecipeDef recipe)
        {
            return recipe.products != null && recipe.products.Count > 0;
        }

        private int GetBillId(Bill_Production bill)
        {
            return (int)LoadIdGetter.GetValue(bill);
        }
    }
}