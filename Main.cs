using HugsLib.Settings;
using HugsLib.Utils;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    public class Main : HugsLib.ModBase
    {
        public Main()
        {
            Instance = this;
        }

        public override void WorldLoaded()
        {
            base.WorldLoaded();
            Logger.Message("Loading ExtendedBillDataStorage");
            _extendedBillDataStorage =
                UtilityWorldObjectManager.GetUtilityWorldObject<ExtendedBillDataStorage>();
        }

        public override void DefsLoaded()
        {
            _expandBillsTab = Settings.GetHandle(
                "expandBillsTab", "Automatically open bills tab", 
                "When a workbench is selected, its Bills tab will be opened immediately", true);
        }

        public bool ShouldExpandBillsTab()
        {
            return _expandBillsTab;
        }

        public ExtendedBillData GetExtendedDataFor(Bill_Production bill)
        {
            return _extendedBillDataStorage?.GetExtendedDataFor(bill);
        }

        public void DeleteExtendedDataFor(Bill_Production bill)
        {
            Main.Instance.Logger.Message($"Deleting extended data for {bill.GetUniqueLoadID()}");
            _extendedBillDataStorage?.DeleteExtendedDataFor(bill);
        }

        private ExtendedBillDataStorage _extendedBillDataStorage;

        internal new ModLogger Logger => base.Logger;

        internal static Main Instance { get; private set; }

        public override string ModIdentifier => "ImprovedWorkbenches";

        private SettingHandle<bool> _expandBillsTab;
    }
}
