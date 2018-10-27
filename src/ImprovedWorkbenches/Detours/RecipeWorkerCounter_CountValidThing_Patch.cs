using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(RecipeWorkerCounter), "CountValidThing")]
    class RecipeWorkerCounter_CountValidThing_Patch
    {
        //public bool CountValidThing(Thing thing, Bill_Production bill, ThingDef def)
        public static void Postfix(ref bool __result, Thing thing, Bill_Production bill)
        {
            if (!__result) return;


            var extendedBillData = Main.Instance.GetExtendedBillDataStorage().GetExtendedDataFor(bill);
            if(extendedBillData.OutputFilter != null)
            {
                Log.Message($"Applying {extendedBillData.OutputFilter} to {thing}");
                __result = extendedBillData.OutputFilter.Allows(thing);
            }
        }
    }
}
