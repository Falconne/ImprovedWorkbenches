using UnityEngine;
using Verse;

namespace ImprovedWorkbenches
{
    [StaticConstructorOnStartup]
    public class Resources
    {
        public static readonly Texture2D CopyButton = ContentFinder<Texture2D>.Get("UI/Buttons/Copy");

        public static readonly Texture2D DragHash = ContentFinder<Texture2D>.Get("UI/Buttons/DragHash", true);

        public static readonly Texture2D BreakLink = ContentFinder<Texture2D>.Get("BreakLink");

        public static readonly Texture2D LeftArrow = ContentFinder<Texture2D>.Get("LeftArrow");

        public static readonly Texture2D RightArrow = ContentFinder<Texture2D>.Get("RightArrow");

        public static readonly Texture2D Rename = ContentFinder<Texture2D>.Get("UI/Buttons/Rename");

        public static readonly Texture2D DropOnFloor = ContentFinder<Texture2D>.Get("DropOnFloor");

        public static readonly Texture2D BestStockpile = ContentFinder<Texture2D>.Get("BestStockpile");
    }
}