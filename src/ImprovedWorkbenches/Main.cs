using System;
using System.Collections.Generic;
using HarmonyLib;
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
            _extendedBillDataStorage = Find.World.GetComponent<ExtendedBillDataStorage>();
            BillCopyPasteHandler.Clear();
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

            _mirrorSuspendedStatus.ValueChanged += handle =>
            {
                if (_mirrorSuspendedStatus.Value)
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

            IntegrateWithNoMaxBills();
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
                var assemblers = GenTypes.GetTypeInAnyAssembly(
                    "ProjectRimFactory.SAL3.Things.Assemblers.Building_DynamicBillGiver");
                var drills = GenTypes.GetTypeInAnyAssembly(
                    "ProjectRimFactory.AutoMachineTool.Building_Miner");

                //quick exit: if we don't find types, we aren't going to integrate
                if (assemblers is null && drills is null)
                    return;

                Logger.Message("Adding support for ProjectRimFactory");

                //register the buildings
                if (assemblers != null)
                    _rimFactoryBuildings.Add(assemblers);
                if (drills != null)
                    _rimFactoryBuildings.Add(drills);

                _isRimfactoryLoaded = true;
            }
            catch (Exception e)
            {
                Logger.Error("Exception while trying to detect RimFactory:");
                Logger.Error(e.Message);
                Logger.Error(e.StackTrace);
            }
        }

        private void IntegrateWithNoMaxBills()
        {
            try
            {
                if (GenTypes.GetTypeInAnyAssembly("NoMaxBills.Patch_BillStack_DoListing") == null)
                    return;

                Logger.Message("Adding support for No Max Bills");
                _isNoMaxBillsLoaded = true;
            }
            catch (Exception e)
            {
                Logger.Error("Exception while trying to detect NoMaxBills:");
                Logger.Error(e.Message);
                Logger.Error(e.StackTrace);
            }
        }

        public bool IsOfTypeRimFactoryBuilding(Thing obj)
        {
            var type = obj?.GetType();
            return _isRimfactoryLoaded && _rimFactoryBuildings.Any(x => x == type || (type?.IsSubclassOf(x) ?? false));
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

        public int GetMaxBills()
        {
            return _isNoMaxBillsLoaded ? 125 : BillStack.MaxCount;
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

        private List<Type> _rimFactoryBuildings = new List<Type>();

        private bool _isNoMaxBillsLoaded;
    }
}
