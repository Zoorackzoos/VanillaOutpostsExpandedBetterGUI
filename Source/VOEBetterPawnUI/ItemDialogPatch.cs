using HarmonyLib;
using Outposts;
using UnityEngine;
using Verse;

namespace VOEBetterPawnUI
{
    [HarmonyPatch]
    public static class ItemDialogPatch
    {
        // The original draw order is: DoBottomButtons(rect), then itemsTransfer.OnGUI(rect)
        // Both get the same rect so the list draws over the buttons.
        // We prefix DoWindowContents to shrink inRect.yMax before anything draws,
        // which makes both the button positions AND the list respect the boundary.

        private const float ButtonAreaH = 60f;

        [HarmonyPatch(typeof(Dialog_TakeItems), nameof(Dialog_TakeItems.DoWindowContents))]
        [HarmonyPrefix]
        public static void TakeItems_ShrinkRect(ref Rect inRect)
        {
            inRect.yMax -= ButtonAreaH;
        }

        [HarmonyPatch(typeof(Dialog_GiveItems), nameof(Dialog_GiveItems.DoWindowContents))]
        [HarmonyPrefix]
        public static void GiveItems_ShrinkRect(ref Rect inRect)
        {
            inRect.yMax -= ButtonAreaH;
        }
    }
}