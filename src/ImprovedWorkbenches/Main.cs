using System;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using static ImprovedWorkbenches.ModSettings_ImprovedWorkbenches;

namespace ImprovedWorkbenches
{
    [StaticConstructorOnStartup]
    public class Main
    {
        public Main(Harmony harmony)
        {
            Instance = this;
            // Integration with other mods
            IntegrateWithOutfitter(harmony);

            IntegrateWithRimFactory();

            IntegrateWithNoMaxBills();
        }

        [HarmonyPatch(typeof(World), nameof(World.FinalizeInit))]
        static class Patch_FinalizeInit
        {
            static void Postfix()
            {
                Main.Instance._extendedBillDataStorage = Find.World.GetComponent<ExtendedBillDataStorage>();
                Main.Instance.BillCopyPasteHandler.Clear();
            }
        }

        private void IntegrateWithOutfitter(Harmony harmony)
        {
            try
            {
                var outfitterBillsPatcher = GenTypes.GetTypeInAnyAssembly("Outfitter.TabPatch.ITab_Bills_Patch");
                if (outfitterBillsPatcher == null)
                    return;

                Log.Message("[Better Workbench Management] Adding support for Outfitter");
                var outfitterPatchedMethod = outfitterBillsPatcher.GetMethod("DoListing");
                var ourPrefix = typeof(BillStack_DoListing_Detour).GetMethod("Prefix");
                var ourPostfix = typeof(BillStack_DoListing_Detour).GetMethod("Postfix");
                harmony.Patch(outfitterPatchedMethod, new HarmonyMethod(ourPrefix), new HarmonyMethod(ourPostfix));
            }
            catch (Exception e)
            {
                Log.Message("[Better Workbench Management] Exception while trying to detect Outfitter:");
                Log.Error("[Better Workbench Management] " + e.Message);
                Log.Error("[Better Workbench Management] " + e.StackTrace);
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

                Log.Message("[Better Workbench Management] Adding support for ProjectRimFactory");
                _rimFactoryBuildingType = GenTypes.GetTypeInAnyAssembly(
                    "ProjectRimFactory.SAL3.Things.Assemblers.Building_DynamicBillGiver");
                _isRimfactoryLoaded = true;
            }
            catch (Exception e)
            {
                Log.Message("[Better Workbench Management] Exception while trying to detect RimFactory:");
                Log.Error("[Better Workbench Management] " + e.Message);
                Log.Error("[Better Workbench Management] " + e.StackTrace);
            }

        }

        private void IntegrateWithNoMaxBills()
        {
            try
            {
                if (GenTypes.GetTypeInAnyAssembly("NoMaxBills.Patch_BillStack_DoListing") == null)
                    return;

                Log.Message("[Better Workbench Management] Adding support for No Max Bills");
                _isNoMaxBillsLoaded = true;
            }
            catch (Exception e)
            {
                Log.Message("[Better Workbench Management] Exception while trying to detect NoMaxBills:");
                Log.Error("[Better Workbench Management] " + e.Message);
                Log.Error("[Better Workbench Management] " + e.StackTrace);
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

        public int GetMaxBills()
        {
            return _isNoMaxBillsLoaded ? 125 : BillStack.MaxCount;
        }

        internal static Main Instance { get; private set; }

        public readonly BillCopyPaste BillCopyPasteHandler = new BillCopyPaste();

        private ExtendedBillDataStorage _extendedBillDataStorage;

        // RImFactory support
        private bool _isRimfactoryLoaded;

        private Type _rimFactoryBillsTabType;

        private Type _rimFactoryBuildingType;

        private bool _isNoMaxBillsLoaded;
    }
}
