using HugsLib.Utils;

namespace ImprovedWorkbenches
{
    public class Main : HugsLib.ModBase
    {
        public Main()
        {
            Instance = this;
        }

        internal new ModLogger Logger => base.Logger;

        internal static Main Instance { get; private set; }

        public override string ModIdentifier => "ImprovedWorkbenches";
    }
}
