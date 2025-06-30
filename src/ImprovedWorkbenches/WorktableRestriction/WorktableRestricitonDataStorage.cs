using Verse;
using RimWorld.Planet;
using System.Collections.Generic;

namespace ImprovedWorkbenches
{
    // This WorldComponent is responsible for saving and loading worktable restriction data for all worktables in the world.
    public class WorktableRestrictionDataStorage : WorldComponent
    {
        private Dictionary<int, WorktableRestrictionData> _worktableRestrictionDictonary = new Dictionary<int, WorktableRestrictionData>();

        public WorktableRestrictionDataStorage(World world) : base(world) { }

        public override void ExposeData()
        {
            base.ExposeData();

            List<int> workTableIds = new List<int>();
            List<WorktableRestrictionData> componentData = new List<WorktableRestrictionData>();

            // For saving, we need to split the dictionary into two lists
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                workTableIds.AddRange(_worktableRestrictionDictonary.Keys);
                componentData.AddRange(_worktableRestrictionDictonary.Values);
            }
            Scribe_Collections.Look(ref _worktableRestrictionDictonary, "worktableRestrictionDictonary", LookMode.Value, LookMode.Deep, ref workTableIds, ref componentData);
        }

        /// <summary>
        /// Retrieves the worktable restriction data for a specific worktable.
        /// </summary>
        /// <param name="workTableId">The unique identifier of the worktable.</param>
        /// <returns>The worktable restriction data if found; otherwise, a new WorktableRestrictionData is created and returned.</returns>
        public WorktableRestrictionData GetWorktableRestrictionData(int workTableId)
        {
            if (_worktableRestrictionDictonary.TryGetValue(workTableId, out WorktableRestrictionData data))
                return data;

            //Create new WorktableRestrictionData and add it to the dictionary
            SetWorktableRestrictionData(workTableId, new WorktableRestrictionData());
            return GetWorktableRestrictionData(workTableId);
        }

        /// <summary>
        /// Adds or updates the worktable restriction data for a specific worktable.
        /// </summary>
        /// <param name="workTableId">The unique identifier of the worktable.</param>
        /// <param name="data">The worktable restriction data to store.</param>
        public void SetWorktableRestrictionData(int workTableId, WorktableRestrictionData data)
        {
            if (_worktableRestrictionDictonary.ContainsKey(workTableId))
                _worktableRestrictionDictonary[workTableId] = data;
            else
                _worktableRestrictionDictonary.Add(workTableId, data);
        }

        /// <summary>
        /// Removes the worktable restriction data for a specific worktable.
        /// </summary>
        /// <param name="workTableId">The unique identifier of the worktable.</param>
        public void RemoveWorktableRestrictionData(int workTableId)
        {
            if (_worktableRestrictionDictonary.ContainsKey(workTableId))
                _worktableRestrictionDictonary.Remove(workTableId);
        }
    }
}