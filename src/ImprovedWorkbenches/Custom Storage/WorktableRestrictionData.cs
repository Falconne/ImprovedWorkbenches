using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ImprovedWorkbenches
{
    // Component that saves worktable restrictions for each worktable
    public class WorktableRestrictionData : IExposable
    {
        public bool isRestricted = false;
        public Pawn restrictionPawn = null;
        public bool restrictionSlavesOnly = false;
        public bool restrictionMechsOnly = false;
        public bool restrictionNonMechsOnly = false;
        public IntRange restrictionAllowedSkillRange = new IntRange(0, 20);

        public WorktableRestrictionData()
        {
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref isRestricted, "isRestricted", false);
            Scribe_References.Look(ref restrictionPawn, "restrictionPawn");
            Scribe_Values.Look(ref restrictionSlavesOnly, "restrictionSlavesOnly", false);
            Scribe_Values.Look(ref restrictionMechsOnly, "restrictionMechsOnly", false);
            Scribe_Values.Look(ref restrictionNonMechsOnly, "restrictionNonMechsOnly", false);
            Scribe_Values.Look(ref restrictionAllowedSkillRange, "restrictionAllowedSkillRange", new IntRange(0, 20));
        }

        public void SetWorktableRestrictionToBill(Bill_Production bill)
        {
            if (restrictionPawn != null)
            {
                bill.SetPawnRestriction(restrictionPawn);
            }
            else if (restrictionSlavesOnly)
            {
                bill.SetAnySlaveRestriction();
                bill.allowedSkillRange = restrictionAllowedSkillRange;
            }
            else if (restrictionMechsOnly)
            {
                bill.SetAnyMechRestriction();
            }
            else if (restrictionNonMechsOnly)
            {
                bill.SetAnyNonMechRestriction();
                bill.allowedSkillRange = restrictionAllowedSkillRange;
            }
            else
            {
                bill.SetAnyPawnRestriction();
                bill.allowedSkillRange = restrictionAllowedSkillRange;
            }
        }
    }
}