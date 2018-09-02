using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HugsLib.Utils;
using ImprovedWorkbenches;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    public class ExtendedBillDataStorage : UtilityWorldObject
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

        // Return the associate extended data for a given bill, if found.
        public ExtendedBillData GetExtendedDataFor(Bill_Production bill)
        {
            var loadId = GetBillId(bill);
            return _store.TryGetValue(loadId, out ExtendedBillData data) ? data : null;
        }

        // Return the associate extended data for a given bill, creating a new association
        // if required.
        public ExtendedBillData GetOrCreateExtendedDataFor(Bill_Production bill)
        {
            var data = GetExtendedDataFor(bill);
            if (data != null)
            {
                return data;
            }

            var newExtendedData = new ExtendedBillData();

            var loadId = GetBillId(bill);
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
            if (bill == null)
                return null;

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

        public void UpdateAllLinkedBills()
        {
            foreach (LinkedBillsSet linkedBillsSet in _linkedBillsSets)
            {
                MirrorBillToLinkedBills(linkedBillsSet.Bills.First());
            }
        }

        public void MirrorBillToLinkedBills(Bill_Production sourceBill)
        {
            var existingBillSet = GetBillSetContaining(sourceBill);
            if (existingBillSet == null)
                return;

            foreach (var linkedBill in existingBillSet.Bills)
            {
                if (linkedBill.DeletedOrDereferenced)
                    continue;

                if (linkedBill == sourceBill)
                    continue;

                MirrorBills(sourceBill, linkedBill, false);
            }
        }

        public void MirrorBills(Bill_Production sourceBill, Bill_Production destinationBill, bool preserveTargetProduct)
        {
            if (!preserveTargetProduct || DoFiltersMatch(sourceBill, destinationBill))
            {
                if (sourceBill.ingredientFilter != null)
                    destinationBill.ingredientFilter?.CopyAllowancesFrom(sourceBill.ingredientFilter);
            }

            destinationBill.ingredientSearchRadius = sourceBill.ingredientSearchRadius;
            destinationBill.allowedSkillRange = sourceBill.allowedSkillRange;
            destinationBill.SetStoreMode(sourceBill.GetStoreMode());
            destinationBill.paused = sourceBill.paused;

            if (Main.Instance.ShouldMirrorSuspendedStatus())
            {
                destinationBill.suspended = sourceBill.suspended;
            }

            var outputCanBeFiltered = 
                CanOutputBeFiltered(destinationBill) 
                || destinationBill.recipe?.WorkerCounter is RecipeWorkerCounter_MakeStoneBlocks;

            if (sourceBill.repeatMode != BillRepeatModeDefOf.TargetCount || outputCanBeFiltered)
            {
                destinationBill.repeatMode = sourceBill.repeatMode;
            }

            if (outputCanBeFiltered)
            {
                destinationBill.repeatCount = sourceBill.repeatCount;
                destinationBill.targetCount = sourceBill.targetCount;
                destinationBill.pauseWhenSatisfied = sourceBill.pauseWhenSatisfied;
                destinationBill.unpauseWhenYouHave = sourceBill.unpauseWhenYouHave;
                destinationBill.includeEquipped = sourceBill.includeEquipped;
                destinationBill.includeTainted = sourceBill.includeTainted;
                destinationBill.includeFromZone = sourceBill.includeFromZone;
                destinationBill.hpRange = sourceBill.hpRange;
                destinationBill.qualityRange = sourceBill.qualityRange;
                destinationBill.limitToAllowedStuff = sourceBill.limitToAllowedStuff;
            }

            var sourceExtendedData = GetOrCreateExtendedDataFor(sourceBill);

            if (sourceExtendedData == null)
                return;

            var destinationExtendedData = GetOrCreateExtendedDataFor(destinationBill);

            destinationExtendedData?.CloneFrom(sourceExtendedData, !preserveTargetProduct);
        }

        // Figure out if output of bill produces a "thing" we care about
        public static bool CanOutputBeFiltered(Bill_Production bill)
        {
            if (bill.recipe == null)
                return false;

            if (bill.recipe.specialProducts == null && bill.recipe.products != null)
                return bill.recipe.products.Count == 1;

            return false;
        }

        private int GetBillId(Bill_Production bill)
        {
            return (int)LoadIdGetter.GetValue(bill);
        }

        private bool DoFiltersMatch(Bill sourceBill, Bill destinationBill)
        {
            var first = sourceBill.recipe?.fixedIngredientFilter;
            var second = destinationBill.recipe?.fixedIngredientFilter;

            if (first == null || second == null)
                return false;

            // Only matching on allowed things for performance. May need to match
            // other fields in the future;
            if (first.AllowedDefCount != second.AllowedDefCount)
                return false;

            foreach (var thingDef in first.AllowedThingDefs)
            {
                if (first.Allows(thingDef) != second.Allows(thingDef))
                {
                    return false;
                }
            }

            return true;
        }
    }
}