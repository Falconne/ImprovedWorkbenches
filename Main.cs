using HugsLib.Settings;
using HugsLib.Utils;

namespace ImprovedWorkbenches
{
    public class Main : HugsLib.ModBase
    {
        public Main()
        {
            Instance = this;
        }

        public override void DefsLoaded()
        {
            _expandBillsTab = Settings.GetHandle(
                "expandBillsTab", "Automatically open bills tab", 
                "When a workbench is selected, its Bills tab will be opened immediately", true);
        }

        public bool ShouldExpandBillsTab()
        {
            return _expandBillsTab;
        }

        public ExtendedBillDataStorage ExtendedBillDataStorage = new ExtendedBillDataStorage();

        internal new ModLogger Logger => base.Logger;

        internal static Main Instance { get; private set; }

        public override string ModIdentifier => "ImprovedWorkbenches";

        private SettingHandle<bool> _expandBillsTab;
    }
}
