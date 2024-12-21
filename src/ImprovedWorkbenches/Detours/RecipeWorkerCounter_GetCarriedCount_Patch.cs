using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(RecipeWorkerCounter))]
    public static class RecipeWorkerCounter_GetCarriedCount_Patch
    {
        [HarmonyPatch(nameof(GetCarriedCount))]
        public static IEnumerable<CodeInstruction> GetCarriedCount(IEnumerable<CodeInstruction> instructions)
        {
            bool found = false;
            foreach( var instruction in instructions )
            {
                if( instruction.opcode == OpCodes.Callvirt
                    && instruction.operand.ToString().EndsWith( "Verse.MapPawns::get_FreeColonistsSpawned()" ))
                {
                    found = true;
                    yield return new CodeInstruction(OpCodes.Call,
                        typeof(RecipeWorkerCounter_GetCarriedCount_Patch).GetMethod(nameof(GetCarriedCount_Hook)));
                }
                else
                    yield return instruction;
            }
            if( !found )
                Log.Error( "ImprovedWorkbenches: GetCarriedCount transpiller failed." );
        }

        private static List<Pawn> cachedResult = new List<Pawn>();

        private static List<Pawn> GetCarriedCount_Hook(MapPawns mapPawns)
        {
            if (!Main.Instance.ShouldCountCarriedByNonHumans())
                return mapPawns.FreeColonistsSpawned;
            cachedResult.Clear();
            cachedResult.AddRange(mapPawns.FreeColonistsSpawned);
            cachedResult.AddRange(mapPawns.SpawnedColonyAnimals);
            cachedResult.AddRange(mapPawns.SpawnedColonyMechs);
            cachedResult.AddRange(mapPawns.SpawnedColonyMutantsPlayerControlled);
            return cachedResult;
        }
    }
}
