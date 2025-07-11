using Verse;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using RimWorld;

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
        #region XML Comment
        /// <summary>
        /// Retrieves the worktable restriction data for a specific worktable.
        /// </summary>
        /// <param name="workTableId">The unique identifier of the worktable.</param>
        /// <returns>The worktable restriction data if found; otherwise, a new WorktableRestrictionData is created and returned.</returns>
        #endregion
        public WorktableRestrictionData GetWorktableRestrictionData(int workTableId)
        {
            if (_worktableRestrictionDictonary.TryGetValue(workTableId, out WorktableRestrictionData data))
                return data;

            //Create new WorktableRestrictionData and add it to the dictionary
            SetWorktableRestrictionData(workTableId, new WorktableRestrictionData());
            return GetWorktableRestrictionData(workTableId);
        }
        #region XML Comment
        /// <summary>
        /// Retrieves the worktable restriction data for a specific bill.
        /// </summary>
        /// <param name="bill">The bill to get the worktable restriction data for.</param>
        /// <returns>The worktable restriction data if found; otherwise, null.</returns>
        #endregion
        public WorktableRestrictionData GetWorktableRestrictionData(Bill_Production bill)
        {
            if (bill?.billStack?.billGiver is Building_WorkTable workTable)
                return GetWorktableRestrictionData(workTable.thingIDNumber);

            return null;
        }

        #region XML Comment
        /// <summary>
        /// Adds or updates the worktable restriction data for a specific worktable.
        /// </summary>
        /// <param name="workTableId">The unique identifier of the worktable.</param>
        /// <param name="data">The worktable restriction data to store.</param>
        #endregion
        public void SetWorktableRestrictionData(int workTableId, WorktableRestrictionData data)
        {
            if (_worktableRestrictionDictonary.ContainsKey(workTableId))
                _worktableRestrictionDictonary[workTableId] = data;
            else
                _worktableRestrictionDictonary.Add(workTableId, data);
        }

        #region XML Comment
        /// <summary>
        /// Removes the worktable restriction data for a specific worktable.
        /// </summary>
        /// <param name="workTableId">The unique identifier of the worktable.</param>
        #endregion
        public void RemoveWorktableRestrictionData(int workTableId)
        {
            if (_worktableRestrictionDictonary.ContainsKey(workTableId))
                _worktableRestrictionDictonary.Remove(workTableId);
        }
    }
}