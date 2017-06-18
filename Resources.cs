using UnityEngine;
using Verse;

namespace ImprovedWorkbenches
{
    [StaticConstructorOnStartup]
    public class Resources
    {
        public static Texture2D CopyButton = ContentFinder<Texture2D>.Get("UI/Buttons/Copy", true);
    }
}