using System;
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
                "expandBillsTab", "IW.AutoOpenBillTabLabel".Translate(),
                "IW.AutoOpenBillTabDesc".Translate(), true);

            _showIngredientCount = Settings.GetHandle(
                "showIngredientCount", "IW.ShowItemCountInFilterLabel".Translate(),
                "IW.ShowItemCountInFilterDesc".Translate(), true);

            _enableDragToReorder = Settings.GetHandle(
                "enableDragToReorder", "IW.EnableDragToReorder".Translate(),
                "IW.EnableDragToReorderDesc".Translate(), true);

            try
            {

                _isOutfitterModLoaded = GenTypes.GetTypeInAnyAssembly("Outfitter.Controller") != null;
            }
            catch (Exception e)
            {
                Logger.Error("Exception while trying to detect Outfitter:");
                Logger.Error(e.Message);
                Logger.Error(e.StackTrace);
            }
        }

        public bool ShouldExpandBillsTab()
        {
            return _expandBillsTab;
        }

        public bool ShouldAllowDragToReorder()
        {
            if (_isOutfitterModLoaded)
                return false;

            return _enableDragToReorder;
        }

        public void OnProductionDialogBeingShown()
        {
            IsRootBillFilterBeingDrawn = _showIngredientCount;
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

        private SettingHandle<bool> _enableDragToReorder;

        private bool _isOutfitterModLoaded;

        private ExtendedBillDataStorage _extendedBillDataStorage;
    }
}
