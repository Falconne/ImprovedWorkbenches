using Verse;
using UnityEngine;
using HarmonyLib;
using static ImprovedWorkbenches.ModSettings_ImprovedWorkbenches;
 
namespace ImprovedWorkbenches
{
    public class Mod_ImprovedWorkbenches : Mod
	{
		public Mod_ImprovedWorkbenches(ModContentPack content) : base(content)
		{
			var harmony = new Harmony(this.Content.PackageIdPlayerFacing);
			new Main(harmony);
			harmony.PatchAll();
			base.GetSettings<ModSettings_ImprovedWorkbenches>();
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			Listing_Standard options = new Listing_Standard();
			options.Begin(inRect);
			options.CheckboxLabeled("IW.AutoOpenBillTabLabel".Translate(), ref _expandBillsTab, "IW.AutoOpenBillTabDesc".Translate());
			options.CheckboxLabeled("IW.EnableDragToReorder".Translate(), ref _enableDragToReorder, "IW.EnableDragToReorderDesc".Translate());
			options.CheckboxLabeled("IW.MirrorSuspendedStatus".Translate(), ref _mirrorSuspendedStatus, "IW.MirrorSuspendedStatusDesc".Translate());
			options.CheckboxLabeled("IW.DropOnFloorByDefault".Translate(), ref _dropOnFloorByDefault, "IW.DropOnFloorByDefaultDesc".Translate());
			options.CheckboxLabeled("IW.CountOutsideStockpiles".Translate(), ref _countOutsideStockpiles, "IW.CountOutsideStockpilesDesc".Translate());
            options.CheckboxLabeled("IW.CountCarriedByNonHumans".Translate(), ref _countCarriedByNonHumans, "IW.CountCarriedByNonHumansDesc".Translate());
			options.End();
			base.DoSettingsWindowContents(inRect);
		}
		public override string SettingsCategory()
		{
			return "Better Workbench Management";
		}
		public override void WriteSettings()
		{
			base.WriteSettings();

			//_extendedBillDataStorage?.UpdateAllLinkedBills();
                
		}
	}
	public class ModSettings_ImprovedWorkbenches : ModSettings
	{
		public override void ExposeData()
		{
			Scribe_Values.Look<bool>(ref _expandBillsTab, "expandBillsTab", true);
			Scribe_Values.Look<bool>(ref _enableDragToReorder, "enableDragToReorder", true);
			Scribe_Values.Look<bool>(ref _mirrorSuspendedStatus, "mirrorSuspendedStatus", true);
			Scribe_Values.Look<bool>(ref _dropOnFloorByDefault, "dropOnFloorByDefault");
			Scribe_Values.Look<bool>(ref _countOutsideStockpiles, "countOutsideStockpiles", true);
			base.ExposeData();
		}

		public static bool  _expandBillsTab = true,
			_enableDragToReorder = true,
			_mirrorSuspendedStatus = true,
			_dropOnFloorByDefault,
			_countOutsideStockpiles = true,
			_countCarriedByNonHumans = true;
	}
}