using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace ImprovedWorkbenches
{
    [HarmonyPatch(typeof(BillStack), "DoListing")]
    public class BillStack_DoListing_Detour
    {
        public static int ReorderableGroup { get; private set; }

        public static bool BlockButtonDraw = false;

        private static readonly FieldInfo WinSizeGetter = typeof(ITab_Bills).GetField("WinSize",
            BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly FieldInfo PasteXGetter = typeof(ITab_Bills).GetField("PasteX",
            BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly FieldInfo PasteYGetter = typeof(ITab_Bills).GetField("PasteY",
            BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly FieldInfo PasteSizeGetter = typeof(ITab_Bills).GetField("PasteSize",
            BindingFlags.NonPublic | BindingFlags.Static);

        private static Rect _vanillaPasteRect;

        public static bool Prefix()
        {
            var selectedThing = Find.Selector.SingleSelectedThing;
            var billGiver = selectedThing as IBillGiver;
            if (billGiver == null)
                return true;

            //if this is not a workable table or an automated building, exit
            if (!(selectedThing is Building_WorkTable) && !Main.Instance.IsOfTypeRimFactoryBuilding(selectedThing))
                return true;

            if (Main.Instance.ShouldAllowDragToReorder() && Event.current.type == EventType.Repaint)
            {
                ReorderableGroup = ReorderableWidget.NewGroup(
                (from, to) => ReorderBillInStack(billGiver.BillStack, from, to),
                ReorderableDirection.Vertical, 
                new Rect(0f, 0f, UI.screenWidth, UI.screenHeight));
            }

            var winSize = (Vector2) WinSizeGetter.GetValue(null);
            var pasteX = (float) PasteXGetter.GetValue(null);
            var pasteY = (float) PasteYGetter.GetValue(null);
            var buttonWidth = (float) PasteSizeGetter.GetValue(null);
            _vanillaPasteRect = new Rect(winSize.x - pasteX, pasteY, buttonWidth, buttonWidth);

            //exit pasting if not a worktable or automated variant
            if (!GetBillsAndRecipes(selectedThing, out BillStack bills, out List<RecipeDef> recipes))
                return true;

            var billCopyPasteHandler = Main.Instance.BillCopyPasteHandler;
            if (!billCopyPasteHandler.CanPasteInto(bills, recipes))
                return true;

            if (Widgets.ButtonImageFitted(_vanillaPasteRect, Resources.PasteButton, Color.white))
            {
                billCopyPasteHandler.DoPasteInto(bills, recipes, false);
            }

            return true;
        }

        static bool GetBillsAndRecipes(Thing selectedThing, out BillStack bills, out List<RecipeDef> recipes)
        {
            bool found = false;
            bills = null;
            recipes = null;

            try
            {
                //work bench
                if (selectedThing is Building_WorkTable workTable)
                {
                    bills = workTable.BillStack;
                    recipes = workTable.def.AllRecipes;
                    found = true;
                }
                //Project RimFactory
                else if (Main.Instance.IsOfTypeRimFactoryBuilding(selectedThing))
                {
                    //reflection pain to get the BillStack
                    var type = selectedThing.GetType();
                    var prop = type.GetProperty("BillStack");
                    bills = prop.GetValue(selectedThing) as BillStack;

                    //common to all Thing instances
                    recipes = selectedThing.def.AllRecipes;
                    found = true;
                }
            }
            catch (Exception ex)
            {
                Main.Instance.Logger.Error("Exception while trying to extract selected item bills and recipes:");
                Main.Instance.Logger.Error(ex.Message);
                Main.Instance.Logger.Error(ex.StackTrace);
            }

            return found;
        }

        static void ReorderBillInStack(BillStack stack, int from, int to)
        {
            if (to >= stack.Count)
                to = stack.Count - 1;

            if (from == to)
                return;

            var bill = stack[from];
            var offset = to - from;
            stack.Reorder(bill, offset);
        }

        public static void Postfix(ref Rect rect)
        {
            //exit pasting if not a worktable or automated variant
            if (!GetBillsAndRecipes(Find.Selector.SingleSelectedThing, out BillStack bills, out List<RecipeDef> recipes))
                return;

            var gap = 4f;
            var buttonWidth = (float) PasteSizeGetter.GetValue(null);

            var buttonRect = new Rect(_vanillaPasteRect);
            buttonRect.xMin -= buttonWidth + gap;
            buttonRect.xMax -= buttonWidth + gap;

            var billCopyPasteHandler = Main.Instance.BillCopyPasteHandler;
            if (bills != null && bills.Count > 0)
            {
                if (Widgets.ButtonImageFitted(buttonRect, Resources.CopyButton, Color.white))
                {
                    billCopyPasteHandler.DoCopy(bills);
					SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                }
                TooltipHandler.TipRegion(buttonRect, "IW.CopyAllTip".Translate());
                buttonRect.xMin -= buttonWidth + gap;
                buttonRect.xMax -= buttonWidth + gap;
            }

            if (!billCopyPasteHandler.CanPasteInto(bills, recipes))
                return;

            var rectPaste = new Rect(_vanillaPasteRect);
            if (Widgets.ButtonImageFitted(rectPaste, Resources.PasteButton, Color.white))
            {
                billCopyPasteHandler.DoPasteInto(bills, recipes, false);
            }

            if (Widgets.ButtonImageFitted(buttonRect, Resources.Link, Color.white))
            {
                billCopyPasteHandler.DoPasteInto(bills, recipes, true);
            }
            TooltipHandler.TipRegion(buttonRect, "IW.PasteLinkTip".Translate());
        }
    }
}