using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    public class ExtendedBillData : IExposable
    {
        public ThingFilter OutputFilter = new ThingFilter();
        public bool AllowDeadmansApparel;
        public bool CountInventory;
        public bool CountInstalled;
        public bool UseInputFilter;
        public Pawn Worker;
        public string Name;

        public Map Map;

        private Zone_Stockpile _countingStockpile;
        private string _countingStockpileName = "null";

        private Zone_Stockpile _takeToStockpile;
        private string _takeToStockpileName = "null";

        public ExtendedBillData()
        {
        }

        // Constructor for migrating old data storage format to new method.
        public ExtendedBillData(Bill_Production bill)
        {
            Map = bill.Map;

            var billWithWorkerFilter = bill as IBillWithWorkerFilter;
            Worker = billWithWorkerFilter.GetWorker();

            if (!ExtendedBillDataStorage.CanOutputBeFiltered(bill))
                return;

            var billWithThingFilter = bill as IBillWithThingFilter;
            if (billWithThingFilter == null)
                return;

            OutputFilter = billWithThingFilter.GetOutputFilter();
            AllowDeadmansApparel = billWithThingFilter.GetAllowDeadmansApparel();
            UseInputFilter = billWithThingFilter.GetUseInputFilter();
        }

        public bool UsesTakeToStockpile()
        {
            return _takeToStockpile != null;
        }

        public string CurrentTakeToStockpileLabel()
        {
            return UsesTakeToStockpile() ? "IW.TakeToLabel".Translate() + " " + _takeToStockpile.label : "IW.BestLabel".Translate();
        }

        public void RemoveTakeToStockpile()
        {
            _takeToStockpile = null;
        }

        public void SetTakeToStockpile(Zone_Stockpile stockpile)
        {
            Map = stockpile.Map;
            _takeToStockpile = stockpile;
        }

        public Zone_Stockpile GetTakeToStockpile()
        {
            return _takeToStockpile;
        }

        public bool UsesCountingStockpile()
        {
            return _countingStockpile != null;
        }

        public void RemoveCountingStockpile()
        {
            _countingStockpile = null;
        }

        public void SetCountingStockpile(Zone_Stockpile stockpile)
        {
            Map = stockpile.Map;
            _countingStockpile = stockpile;
        }

        public Zone_Stockpile GetCountingStockpile()
        {
            return _countingStockpile;
        }

        public IEnumerable<Thing> GetThingsInCountingStockpile()
        {
            return _countingStockpile?.GetSlotGroup()?.HeldThings;
        }

        public void CloneFrom(ExtendedBillData other, bool cloneName)
        {
            OutputFilter.CopyAllowancesFrom(other.OutputFilter);
            AllowDeadmansApparel = other.AllowDeadmansApparel;
            CountInventory = other.CountInventory;
            CountInstalled = other.CountInstalled;
            UseInputFilter = other.UseInputFilter;
            Worker = other.Worker;
            if (this.Map == other.Map)
            {
                _countingStockpile = other._countingStockpile;
                _takeToStockpile = other._takeToStockpile;
            }
            if (cloneName)
                Name = other.Name;
        }

        public void SetDefaultFilter(Bill_Production bill)
        {
            Map = bill.Map;

            var thingDef = bill.recipe.products.First().thingDef;
            OutputFilter.SetDisallowAll();
            OutputFilter.SetAllow(thingDef, true);
        }

        public bool IsHitpointsFilteringNeeded()
        {
            return OutputFilter.allowedHitPointsConfigurable &&
                   OutputFilter.AllowedHitPointsPercents != FloatRange.ZeroToOne;
        }

        public bool IsQualityFilteringNeeded()
        {
            return OutputFilter.allowedQualitiesConfigurable &&
                   OutputFilter.AllowedQualityLevels != QualityRange.All;
        }

        public void ExposeData()
        {
            Scribe_Deep.Look(ref OutputFilter, "outputFilter", new object[0]);
            Scribe_Values.Look(ref AllowDeadmansApparel, "allowDeadmansApparel", false);
            Scribe_Values.Look(ref CountInventory, "countInventory", false);
            Scribe_Values.Look(ref CountInstalled, "countInstalled", true);
            Scribe_Values.Look(ref UseInputFilter, "useInputFilter", false);
            Scribe_References.Look(ref Worker, "worker");
            Scribe_Values.Look(ref Name, "name", null);


            // Backward compatibilty, combine all settings into inventory
            bool CountWornApparel = false, CountEquippedWeapons = false;
            Scribe_Values.Look(ref CountWornApparel, "countWornApparel", false);
            Scribe_Values.Look(ref CountEquippedWeapons, "countEquippedWeapons", false);
            CountInventory = CountInventory || CountWornApparel || CountEquippedWeapons;

            // Stockpiles need special treatment; they cannot be referenced.
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                _countingStockpileName = _countingStockpile?.label ?? "null";
                _takeToStockpileName = _takeToStockpile?.label ?? "null";
            }

            Scribe_Values.Look(ref _countingStockpileName, "countingStockpile", "null");
            Scribe_Values.Look(ref _takeToStockpileName, "takeToStockpile", "null");
            Scribe_References.Look(ref Map, "BillMap");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                _countingStockpile = _countingStockpileName == "null"
                    ? null
                    : Map.zoneManager.AllZones.FirstOrDefault(z =>
                        z is Zone_Stockpile && z.label == _countingStockpileName)
                        as Zone_Stockpile;

                _takeToStockpile = _takeToStockpileName == "null"
                    ? null
                    : Map.zoneManager.AllZones.FirstOrDefault(z =>
                            z is Zone_Stockpile && z.label == _takeToStockpileName)
                        as Zone_Stockpile;
            }
        }
    }
}