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
            if (!Main.Instance.ShouldAllowDragToReorder())
                return true;

            var selectedThing = Find.Selector.SingleSelectedThing;
            var billGiver = selectedThing as IBillGiver;
            if (billGiver == null)
                return true;

            if (!(selectedThing is Building_WorkTable) && !Main.Instance.IsOfTypeRimFactoryBuilding(selectedThing))
                return true;

            ReorderableGroup = ReorderableWidget.NewGroup(
                (from, to) => ReorderBillInStack(billGiver.BillStack, from, to),
                ReorderableDirection.Vertical);

            var winSize = (Vector2) WinSizeGetter.GetValue(null);
            var pasteX = (float) PasteXGetter.GetValue(null);
            var pasteY = (float) PasteYGetter.GetValue(null);
            var buttonWidth = (float) PasteSizeGetter.GetValue(null);
            _vanillaPasteRect = new Rect(winSize.x - pasteX, pasteY, buttonWidth, buttonWidth);

            if (!(selectedThing is Building_WorkTable workTable))
                return true;

            var billCopyPasteHandler = Main.Instance.BillCopyPasteHandler;
            if (!billCopyPasteHandler.CanPasteInto(workTable))
                return true;

            if (Widgets.ButtonImageFitted(_vanillaPasteRect, Resources.PasteButton, Color.white))
            {
                billCopyPasteHandler.DoPasteInto(workTable, false);
            }

            return true;
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
            if (!(Find.Selector.SingleSelectedThing is Building_WorkTable workTable))
                return;

            var gap = 4f;
            var buttonWidth = (float) PasteSizeGetter.GetValue(null);

            var buttonRect = new Rect(_vanillaPasteRect);
            buttonRect.xMin -= buttonWidth + gap;
            buttonRect.xMax -= buttonWidth + gap;

            var billCopyPasteHandler = Main.Instance.BillCopyPasteHandler;
            if (workTable.BillStack != null && workTable.BillStack.Count > 0)
            {
                if (Widgets.ButtonImageFitted(buttonRect, Resources.CopyButton, Color.white))
                {
                    billCopyPasteHandler.DoCopy(workTable);
					SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                }
                TooltipHandler.TipRegion(buttonRect, "IW.CopyAllTip".Translate());
                buttonRect.xMin -= buttonWidth + gap;
                buttonRect.xMax -= buttonWidth + gap;
            }

            if (!billCopyPasteHandler.CanPasteInto(workTable))
                return;

            var rectPaste = new Rect(_vanillaPasteRect);
            if (Widgets.ButtonImageFitted(rectPaste, Resources.PasteButton, Color.white))
            {
                billCopyPasteHandler.DoPasteInto(workTable, false);
            }

            if (Widgets.ButtonImageFitted(buttonRect, Resources.Link, Color.white))
            {
                billCopyPasteHandler.DoPasteInto(workTable, true);
            }
            TooltipHandler.TipRegion(buttonRect, "IW.PasteLinkTip".Translate());
        }
    }
}