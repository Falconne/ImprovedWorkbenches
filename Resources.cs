using UnityEngine;
using Verse;

namespace ImprovedWorkbenches
{
    [StaticConstructorOnStartup]
    public class Resources
    {
        public static Texture2D CopyButton = ContentFinder<Texture2D>.Get("UI/Buttons/Copy");

        public static Texture2D BreakLink = ContentFinder<Texture2D>.Get("BreakLink");

        public static readonly Texture2D Rename = ContentFinder<Texture2D>.Get("UI/Buttons/Rename");
    }
}