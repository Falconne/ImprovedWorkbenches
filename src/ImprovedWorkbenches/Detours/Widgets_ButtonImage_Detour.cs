using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace ImprovedWorkbenches
{
    [HarmonyPatch]
    public class Widgets_ButtonImage_Detour
    {
        [HarmonyTargetMethod]
        public static MethodInfo TargetMethod(Harmony inst)
        {
            try
            {
                return AccessTools.Method(typeof(Widgets), "ButtonImage",
                    new[] { typeof(Rect), typeof(Texture2D), typeof(Color) });

            }
            catch (Exception )
            {
                return null;
            }
        }

        static bool Prefix(ref bool __result, Rect butRect, Texture2D tex, Color baseColor)
        {
            if (!Main.Instance.ShouldAllowDragToReorder())
                return true;

            if (BillStack_DoListing_Detour.BlockButtonDraw)
            {
                __result = false;
                return false;
            }

            return true;
        }
    }
}