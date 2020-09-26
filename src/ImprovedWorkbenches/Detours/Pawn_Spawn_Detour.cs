using HarmonyLib;
using Verse;


namespace ImprovedWorkbenches.Detours
{
    //This ought to be ExitMap, but sometimes DeSpawn is called before it :/
    [HarmonyPatch(typeof(Pawn), "DeSpawn")]
    class Pawn_DeSpawn_Detour
    {
        public static void Prefix(Pawn __instance)
        {
            if ((__instance?.Map?.IsPlayerHome ?? false) && (__instance?.Faction?.IsPlayer ?? false))
            {
                __instance.SetOriginMap(__instance.Map);
            }
        }
    }

    [HarmonyPatch(typeof(Pawn), "SpawnSetup")]
    class Pawn_SpawnSetup_Detour
    {
        public static void Postfix(Pawn __instance, Map map, bool respawningAfterLoad)
        {
            //If NEW ENTERED map is player home, forget ORIGINAL map
            //ie Don't forget original when you are just raiding an enemy base
            if (__instance.HasOriginMap() && map.IsPlayerHome)
            {
                //Pawn is there ; doesn't need to remember it
                __instance.SetOriginMap(null);
            }
        }
    }
}
