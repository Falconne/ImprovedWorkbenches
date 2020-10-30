using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using HarmonyLib;

namespace ImprovedWorkbenches
{
    public class ExtendedBillData : IExposable
    {
        public bool CountAway;
        public string Name;
        public ThingFilter ProductAdditionalFilter;

        public ExtendedBillData()
        {
        }

        public void CloneFrom(ExtendedBillData other, bool cloneName)
        {
            if (other != null)
            {
                CountAway = other.CountAway;
                ProductAdditionalFilter = new ThingFilter();
                if (other.ProductAdditionalFilter != null)
                    ProductAdditionalFilter.CopyAllowancesFrom(other.ProductAdditionalFilter);

                if (cloneName)
                    Name = other.Name;
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref CountAway, "countAway", false);
            Scribe_Values.Look(ref Name, "name", null);
            Scribe_Deep.Look(ref ProductAdditionalFilter, "productFilter");
        }
    }


    [HarmonyPatch(typeof(Bill_Production), nameof(Bill_Production.ExposeData))]
    public static class ExtendedBillData_ExposeData
    {
        public static void Postfix(Bill_Production __instance)
        {
            var storage = HugsLib.Utils.UtilityWorldObjectManager.GetUtilityWorldObject<ExtendedBillDataStorage>();
            storage.GetOrCreateExtendedDataFor(__instance).ExposeData();
        }
    }


    [HarmonyPatch(typeof(Bill_Production), nameof(Bill_Production.Clone))]
    public static class ExtendedBillData_Clone
    {
        public static void Postfix(Bill_Production __instance, Bill __result)
        {
            if (__result is Bill_Production billProduction)
            {
                var storage = Main.Instance.GetExtendedBillDataStorage();
                var sourceExtendedData = storage.GetExtendedDataFor(__instance);
                var destinationExtendedData = storage.GetOrCreateExtendedDataFor(billProduction);

                destinationExtendedData?.CloneFrom(sourceExtendedData, true);
            }
        }
    }

}