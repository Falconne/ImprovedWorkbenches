using HugsLib.Settings;
using HugsLib.Utils;
using RimWorld;
using UnityEngine;
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
            _extendedBillDataStorage =
                UtilityWorldObjectManager.GetUtilityWorldObject<ExtendedBillDataStorage>();
            BillCopyPasteHandler.Clear();
        }

        public override void DefsLoaded()
        {
            _expandBillsTab = Settings.GetHandle(
                "expandBillsTab", "Automatically open bills tab", 
                "When a workbench is selected, its Bills tab will be opened immediately", true);

            _showIngredientCount = Settings.GetHandle(
                "showIngredientCount", "Show item count in filter",
                "Shows item counts in ingredients tab in bill details", true);
        }

        public bool ShouldExpandBillsTab()
        {
            return _expandBillsTab;
        }

        public bool ShouldShowIngredientCount()
        {
            return _showIngredientCount;
        }

        public ExtendedBillDataStorage GetExtendedBillDataStorage()
        {
            return _extendedBillDataStorage;
        }

        internal new ModLogger Logger => base.Logger;

        internal static Main Instance { get; private set; }

        public readonly BillCopyPaste BillCopyPasteHandler = new BillCopyPaste();

        public bool IsRootBillFilterBeingDrawn = false;

        public override string ModIdentifier => "ImprovedWorkbenches";

        private SettingHandle<bool> _expandBillsTab;

        private SettingHandle<bool> _showIngredientCount;

        private ExtendedBillDataStorage _extendedBillDataStorage;
    }
}
