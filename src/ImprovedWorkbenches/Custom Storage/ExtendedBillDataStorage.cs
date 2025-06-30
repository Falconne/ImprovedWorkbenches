using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace ImprovedWorkbenches
{
    public class ExtendedBillDataStorage : WorldComponent
    {
        public ExtendedBillDataStorage(World world) : base(world) { }

        private readonly Dictionary<Bill_Production, ExtendedBillData> _store =
            new Dictionary<Bill_Production, ExtendedBillData>();

        private Dictionary<int, ExtendedBillData> _legacyStore =
            new Dictionary<int, ExtendedBillData>();


        private List<LinkedBillsSet> _linkedBillsSets = new List<LinkedBillsSet>();

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Collections.Look(ref _linkedBillsSets, "linkedBillsSets",
                LookMode.Deep);

            {
                // Needed for migrating legacy ExtendedBillData storage
                var billIDsWorkingList = new List<int>();
                var extendedBillDataWorkingList = new List<ExtendedBillData>();

                Scribe_Collections.Look(
                    ref _legacyStore, "store",
                    LookMode.Value, LookMode.Deep,
                    ref billIDsWorkingList, ref extendedBillDataWorkingList);
            }

            if (Scribe.mode == LoadSaveMode.LoadingVars && _linkedBillsSets == null)
            {
                _linkedBillsSets = new List<LinkedBillsSet>();
            }
        }

        public override void FinalizeInit()
        {
            this.MigrateLegacyBillStore();
        }

        private void MigrateLegacyBillStore()
        {
            if (_legacyStore.Count == 0) return;

            Log.Message("[Better Workbench Management] Migrating legacy bill store");

            var loadIdGetter = typeof(Bill).GetField("loadID", BindingFlags.NonPublic | BindingFlags.Instance);

            if (loadIdGetter == null)
            {
                Log.Message("[Better Workbench Management] Cannot fetch ID field from Bills, cannot migrate legacy data.");
                return;
            }

            var productionBills = _store.Keys.ToList();

            foreach (var billId in _legacyStore.Keys)
            {
                var bill = productionBills.FirstOrDefault(b => billId == (int)loadIdGetter.GetValue(b));
                if (bill == null)
                {
                    Log.Message($"[Better Workbench Management] Cannot find bill for id {billId}, cannot migrate");
                    continue;
                }

                _store[bill] = _legacyStore[billId];
                Log.Message($"[Better Workbench Management] Migradted bill id: {billId}");
            }

            _legacyStore.Clear();
        }


        // Return the associate extended data for a given bill, if found.
        public ExtendedBillData GetExtendedDataFor(Bill_Production bill)
        {
            return _store.TryGetValue(bill, out ExtendedBillData data) ? data : null;
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

            _store[bill] = newExtendedData;
            return newExtendedData;
        }

        // Delete extended data when bill is deleted
        public void DeleteExtendedDataFor(Bill_Production bill)
        {
            RemoveBillFromLinkSets(bill);
            _store.Remove(bill);
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
            destinationBill.SetStoreMode(sourceBill.GetStoreMode(), sourceBill.GetSlotGroup());
            destinationBill.paused = sourceBill.paused;

            if (sourceBill.PawnRestriction != null)
                destinationBill.SetPawnRestriction(sourceBill.PawnRestriction);
            else if (sourceBill.SlavesOnly)
                destinationBill.SetAnySlaveRestriction();
            else if (sourceBill.MechsOnly)
                destinationBill.SetAnyMechRestriction();
            else if (sourceBill.NonMechsOnly)
                destinationBill.SetAnyNonMechRestriction();
            else
                destinationBill.SetAnyPawnRestriction();

            // Colony Groups integration
            if (Main.Instance.ColonyGroupsBillToPawnGroupDictGetter != null)
            {
                try
                {
                    var billToPawnGroupDict = Main.Instance.ColonyGroupsBillToPawnGroupDictGetter();

                    if (billToPawnGroupDict.Contains(sourceBill))
                    {
                        var pawnGroup = billToPawnGroupDict[sourceBill];
                        billToPawnGroupDict[destinationBill] = pawnGroup;
                    }
                    else
                    {
                        billToPawnGroupDict.Remove(destinationBill);
                    }
                }
                catch (Exception e)
                {
                    Log.Error("attempt to copy pawn group assignment of a bill to another bill (ColonyGroups integration)");
                    Log.Error(e.Message);
                }

                if (Main.Instance.ShouldMirrorSuspendedStatus())
                {
                    destinationBill.suspended = sourceBill.suspended;
                }

                var outputCanBeFiltered =
                    CanOutputBeFiltered(destinationBill)
                    || destinationBill.recipe?.WorkerCounter is RecipeWorkerCounter_MakeStoneBlocks
                    || destinationBill.recipe?.WorkerCounter is RecipeWorkerCounter_ButcherAnimals;

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
                    destinationBill.SetIncludeGroup(sourceBill.GetIncludeSlotGroup());
                    destinationBill.hpRange = sourceBill.hpRange;

                    var sourceThingDef = sourceBill.recipe.ProducedThingDef;
                    var producedThingDef = destinationBill.recipe.ProducedThingDef;
                    if (sourceThingDef != null && producedThingDef != null)
                    {
                        if ((sourceThingDef.IsWeapon || sourceThingDef.IsApparel) &&
                            (producedThingDef.IsWeapon || producedThingDef.IsApparel))
                        {
                            destinationBill.includeEquipped = sourceBill.includeEquipped;
                        }

                        if (sourceThingDef.IsApparel && sourceThingDef.apparel.careIfWornByCorpse &&
                            producedThingDef.IsApparel && producedThingDef.apparel.careIfWornByCorpse)
                        {
                            destinationBill.includeTainted = sourceBill.includeTainted;
                        }

                        if (sourceThingDef.HasComp(typeof(CompQuality)) &&
                            producedThingDef.HasComp(typeof(CompQuality)))
                        {
                            destinationBill.qualityRange = sourceBill.qualityRange;
                        }

                        if (sourceThingDef.MadeFromStuff && producedThingDef.MadeFromStuff)
                        {
                            destinationBill.limitToAllowedStuff = sourceBill.limitToAllowedStuff;
                        }
                    }
                }

                var sourceExtendedData = GetOrCreateExtendedDataFor(sourceBill);

                if (sourceExtendedData == null)
                    return;

                var destinationExtendedData = GetOrCreateExtendedDataFor(destinationBill);

                destinationExtendedData?.CloneFrom(sourceExtendedData, !preserveTargetProduct);
            }
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