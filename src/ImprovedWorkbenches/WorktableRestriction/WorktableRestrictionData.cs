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
    public class WorktableRestrictionData
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
    }
}