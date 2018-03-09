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

        private void SetMapForExtendedBillData(int billIdToFind, ExtendedBillData extendedBillData)
        {
            if (extendedBillData.BillMapFoundInSave)
                return;

            Main.Instance.Logger.Message($"Looking for map for bill id {billIdToFind}...");

            // Found a stored dataset with no BillMap. Search through all work tables
            // for the matching bill ID for this dataset and use the map in the bill
            // found.
            foreach (Map someMap in Find.Maps)
            {
                foreach (var workTable in
                    someMap.listerBuildings.AllBuildingsColonistOfClass<Building_WorkTable>())
                {
                    foreach (var bill in workTable.BillStack.Bills)
                    {
                        var billProduction = bill as Bill_Production;
                        if (billProduction == null || GetBillId(billProduction) != billIdToFind)
                            continue;

                        Main.Instance.Logger.Message($"Setting Map for legacy bill store {billIdToFind}");
                        extendedBillData.SetBillMap(billProduction.Map);
                        return;
                    }
                }
            }

            Main.Instance.Logger.Warning($"Bill id {billIdToFind} not found.");
        }

        // Return the associate extended data for a given bill, creating a new association
        // if required.
        public ExtendedBillData GetExtendedDataFor(Bill_Production bill)
        {

            var loadId = GetBillId(bill);
            if (_store.TryGetValue(loadId, out ExtendedBillData data))
            {
                SetMapForExtendedBillData(loadId, data);
                return data;
            }

            var newExtendedData = new ExtendedBillData();
            if (CanOutputBeFiltered(bill))
                newExtendedData.SetDefaultFilter(bill);

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
            if (!preserveTargetProduct || DoFiltersMatch(sourceBill.recipe?.fixedIngredientFilter,
                destinationBill.recipe?.fixedIngredientFilter))
            {
                if (sourceBill.ingredientFilter != null)
                    destinationBill.ingredientFilter?.CopyAllowancesFrom(sourceBill.ingredientFilter);
            }

            destinationBill.ingredientSearchRadius = sourceBill.ingredientSearchRadius;
            destinationBill.allowedSkillRange = sourceBill.allowedSkillRange;
            destinationBill.storeMode = sourceBill.storeMode;
            destinationBill.paused = sourceBill.paused;

            if (Main.Instance.ShouldMirrorSuspendedStatus())
            {
                destinationBill.suspended = sourceBill.suspended;
            }

            if (CanOutputBeFiltered(destinationBill) || sourceBill.repeatMode != BillRepeatModeDefOf.TargetCount)
            {
                destinationBill.repeatMode = sourceBill.repeatMode;
            }

            if (CanOutputBeFiltered(destinationBill))
            {
                destinationBill.repeatCount = sourceBill.repeatCount;
                destinationBill.targetCount = sourceBill.targetCount;
                destinationBill.pauseWhenSatisfied = sourceBill.pauseWhenSatisfied;
                destinationBill.unpauseWhenYouHave = sourceBill.unpauseWhenYouHave;
            }

            var sourceExtendedData = GetExtendedDataFor(sourceBill);

            if (sourceExtendedData == null)
                return;

            var destinationExtendedData = GetExtendedDataFor(destinationBill);

            destinationExtendedData?.CloneFrom(sourceExtendedData, !preserveTargetProduct);
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

        private bool DoFiltersMatch(ThingFilter first, ThingFilter second)
        {
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