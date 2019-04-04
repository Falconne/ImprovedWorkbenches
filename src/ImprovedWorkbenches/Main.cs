using System;
using Harmony;
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
            _extendedBillDataStorage.MigrateLegacyBillStore();
        }

        public override void DefsLoaded()
        {
            _expandBillsTab = Settings.GetHandle(
                "expandBillsTab", "IW.AutoOpenBillTabLabel".Translate(),
                "IW.AutoOpenBillTabDesc".Translate(), true);

            _enableDragToReorder = Settings.GetHandle(
                "enableDragToReorder", "IW.EnableDragToReorder".Translate(),
                "IW.EnableDragToReorderDesc".Translate(), true);

            _mirrorSuspendedStatus = Settings.GetHandle(
                "mirrorSuspendedStatus", "IW.MirrorSuspendedStatus".Translate(),
                "IW.MirrorSuspendedStatusDesc".Translate(), true);

            _mirrorSuspendedStatus.OnValueChanged = value =>
            {
                if (value)
                {
                    _extendedBillDataStorage?.UpdateAllLinkedBills();
                }
            };

            _dropOnFloorByDefault = Settings.GetHandle(
                "dropOnFloorByDefault", "IW.DropOnFloorByDefault".Translate(),
                "IW.DropOnFloorByDefaultDesc".Translate(), false);

            _countOutsideStockpiles = Settings.GetHandle(
                "countOutsideStockpiles", "IW.CountOutsideStockpiles".Translate(),
                "IW.CountOutsideStockpilesDesc".Translate(), true);

            // Integration with other mods

            IntegrateWithOutfitter();

            IntegrateWithRimFactory();
        }

        private void IntegrateWithOutfitter()
        {
            try
            {
                var outfitterBillsPatcher = GenTypes.GetTypeInAnyAssembly("Outfitter.TabPatch.ITab_Bills_Patch");
                if (outfitterBillsPatcher == null)
                    return;

                Logger.Message("Adding support for Outfitter");
                var outfitterPatchedMethod = outfitterBillsPatcher.GetMethod("DoListing");
                var ourPrefix = typeof(BillStack_DoListing_Detour).GetMethod("Prefix");
                var ourPostfix = typeof(BillStack_DoListing_Detour).GetMethod("Postfix");
                HarmonyInst.Patch(outfitterPatchedMethod, new HarmonyMethod(ourPrefix), new HarmonyMethod(ourPostfix));
            }
            catch (Exception e)
            {
                Logger.Error("Exception while trying to detect Outfitter:");
                Logger.Error(e.Message);
                Logger.Error(e.StackTrace);
            }
        }

        private void IntegrateWithRimFactory()
        {
            _isRimfactoryLoaded = false;
            try
            {
                _rimFactoryBillsTabType = GenTypes.GetTypeInAnyAssembly("ProjectRimFactory.SAL3.UI.ITab_SAL3Bills");
                if (_rimFactoryBillsTabType == null)
                    return;

                Logger.Message("Adding support for ProjectRimFactory");
                _rimFactoryBuildingType = GenTypes.GetTypeInAnyAssembly(
                    "ProjectRimFactory.SAL3.Things.Assemblers.Building_DynamicBillGiver");
                _isRimfactoryLoaded = true;
            }
            catch (Exception e)
            {
                Logger.Error("Exception while trying to detect RimFactory:");
                Logger.Error(e.Message);
                Logger.Error(e.StackTrace);
            }

        }

        public bool IsOfTypeRimFactoryBillsTab(InspectTabBase tab)
        {
            return _isRimfactoryLoaded && tab?.GetType() == _rimFactoryBillsTabType;
        }

        public bool IsOfTypeRimFactoryBuilding(Thing obj)
        {
            return _isRimfactoryLoaded && (obj?.GetType().IsSubclassOf(_rimFactoryBuildingType) ?? false);

        }

        public bool ShouldExpandBillsTab()
        {
            return _expandBillsTab;
        }

        public bool ShouldAllowDragToReorder()
        {
            return _enableDragToReorder;
        }

        public bool ShouldMirrorSuspendedStatus()
        {
            return _mirrorSuspendedStatus;
        }

        public bool ShouldDropOnFloorByDefault()
        {
            return _dropOnFloorByDefault;
        }

        public bool ShouldCountOutsideStockpiles()
        {
            return _countOutsideStockpiles;
        }

        public ExtendedBillDataStorage GetExtendedBillDataStorage()
        {
            return _extendedBillDataStorage;
        }

        public void OnBillDeleted(Bill_Production bill)
        {
            _extendedBillDataStorage?.DeleteExtendedDataFor(bill);
            BillCopyPasteHandler.RemoveBill(bill);
        }

        internal new ModLogger Logger => base.Logger;

        internal static Main Instance { get; private set; }

        public readonly BillCopyPaste BillCopyPasteHandler = new BillCopyPaste();

        public override string ModIdentifier => "ImprovedWorkbenches";

        private SettingHandle<bool> _expandBillsTab;

        private SettingHandle<bool> _enableDragToReorder;

        private SettingHandle<bool> _mirrorSuspendedStatus;

        private SettingHandle<bool> _dropOnFloorByDefault;

        private SettingHandle<bool> _countOutsideStockpiles;

        private ExtendedBillDataStorage _extendedBillDataStorage;

        // RImFactory support
        private bool _isRimfactoryLoaded;

        private Type _rimFactoryBillsTabType;

        private Type _rimFactoryBuildingType;
    }
}
