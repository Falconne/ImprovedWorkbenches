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
        public bool CountAway;
        public bool CountInstalled;
        public bool UseInputFilter;
        public Pawn Worker;
        public string Name;

        public bool BillMapFoundInSave = true;

        private Map BillMap;
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
            BillMap = bill.Map;

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

        public void SetBillMap(Map map)
        {
            BillMap = map;
            BillMapFoundInSave = true;
            if (UsesCountingStockpile() && GetCountingStockpile().Map != BillMap)
            {
                Main.Instance.Logger.Warning(
                    $"Resetting ref to counting stockpile {GetCountingStockpile().label} due to map inconsistency.");
                _countingStockpile = FindStockpile(_countingStockpile.label, BillMap);
            }

            if (UsesTakeToStockpile() && GetTakeToStockpile().Map != BillMap)
            {
                Main.Instance.Logger.Warning(
                    $"Resetting ref to take-to stockpile {GetTakeToStockpile().label} due to map inconsistency.");
                _takeToStockpile = FindStockpile(_takeToStockpile.label, BillMap);
            }
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
            BillMap = stockpile.Map;
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
            BillMap = stockpile.Map;
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
            CountAway = other.CountAway;
            CountInstalled = other.CountInstalled;
            UseInputFilter = other.UseInputFilter;
            Worker = other.Worker;
            if (this.BillMap == other.BillMap)
            {
                _countingStockpile = other._countingStockpile;
                _takeToStockpile = other._takeToStockpile;
            }
            if (cloneName)
                Name = other.Name;
        }

        public void SetDefaultFilter(Bill_Production bill)
        {
            BillMap = bill.Map;

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
            Scribe_Values.Look(ref CountAway, "countAway", false);
            Scribe_Values.Look(ref CountInstalled, "countInstalled", false);
            Scribe_Values.Look(ref UseInputFilter, "useInputFilter", false);
            Scribe_References.Look(ref Worker, "worker");
            Scribe_Values.Look(ref Name, "name", null);

            // Stockpiles need special treatment; they cannot be referenced.
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                _countingStockpileName = _countingStockpile?.label ?? "null";
                _takeToStockpileName = _takeToStockpile?.label ?? "null";
            }
            else
            {
                if (!CountInventory)
                {
                    // Read legacy settings on load and migrate them if found
                    bool countWornApparel = false, countEquippedWeapons = false;
                    Scribe_Values.Look(ref countWornApparel, "countWornApparel", false);
                    Scribe_Values.Look(ref countEquippedWeapons, "countEquippedWeapons", false);
                    CountInventory = countWornApparel || countEquippedWeapons;
                }
            }

            Scribe_Values.Look(ref _countingStockpileName, "countingStockpile", "null");
            Scribe_Values.Look(ref _takeToStockpileName, "takeToStockpile", "null");
            Scribe_References.Look(ref BillMap, "BillMap");

            if (Scribe.mode == LoadSaveMode.PostLoadInit &&
                (IsValidStockpileName(_countingStockpileName) || IsValidStockpileName(_takeToStockpileName)))
            {
                // Bill Map will not exist in saves loaded for mod versions prior to 18.9.
                // When migrating old saves, look in all maps for stockpiles. True map will
                // be filled in after ExtendedBillDataStorage is loaded
                if (BillMap == null)
                    BillMapFoundInSave = false;

                _countingStockpile = FindStockpile(_countingStockpileName, BillMap);
                _takeToStockpile = FindStockpile(_takeToStockpileName, BillMap);
            }
        }

        private static bool IsValidStockpileName(string name)
        {
            return !string.IsNullOrEmpty(name) && name != "null";
        }

        private static Zone_Stockpile FindStockpile(string name, Map defaultMap)
        {
            if (!IsValidStockpileName(name))
                return null;

            if (defaultMap != null)
            {
                return defaultMap.zoneManager.AllZones
                    .FirstOrDefault(z => z is Zone_Stockpile && z.label == name)
                    as Zone_Stockpile;
            }

            // No map given, look in all maps, starting with current map, for performance
            var mapsToCheck = new List<Map>() {Find.VisibleMap};
            mapsToCheck.AddRange(Find.Maps);
            foreach (Map someMap in mapsToCheck)
            {
                if (someMap.zoneManager.AllZones
                    .FirstOrDefault(z => z is Zone_Stockpile && z.label == name)
                    is Zone_Stockpile possibleStockpile)
                {
                    return possibleStockpile;
                }
            }

            return null;
        }
    }
}