using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;
using RimWorld;

namespace ImprovedWorkbenches
{
    public class CompPawnOriginalMap : ThingComp
    {
        public Map OriginMap = null;

        public bool HasOriginMap()
        {
            return OriginMap != null;
        }

        public void SetOriginMap(Map map)
        {
            OriginMap = map;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            if (parent.Faction?.IsPlayer ?? false)
            {
                Scribe_References.Look(ref OriginMap, "OriginMap");
            }
        }
    }

    public static class PawnExtensions
    {
        public static bool HasOriginMap(this Pawn pawn)
        {
            return (pawn.GetComp<CompPawnOriginalMap>()?.HasOriginMap() ?? false);
        }

        public static void SetOriginMap(this Pawn pawn, Map map)
        {
            pawn.GetComp<CompPawnOriginalMap>()?.SetOriginMap(map);
        }

        public static Map GetOriginMap(this Pawn pawn)
        {
            return (pawn.GetComp<CompPawnOriginalMap>()?.OriginMap ?? null);
        }
    }
}
