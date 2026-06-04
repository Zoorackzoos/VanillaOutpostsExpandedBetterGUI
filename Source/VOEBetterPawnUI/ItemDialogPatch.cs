using System.Collections.Generic;
using HarmonyLib;
using Outposts;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace VOEBetterPawnUI
{
    /// <summary>
    /// Patches Dialog_TakeItems and Dialog_GiveItems so the scrollable item
    /// list cannot draw over the bottom buttons.
    ///
    /// Strategy: Prefix DoWindowContents to do the CORRECT drawing ourselves,
    /// then return false to skip the broken original.
    /// We replicate the button logic inline using AccessTools for the private methods.
    /// </summary>
    [HarmonyPatch]
    public static class ItemDialogPatch
    {
        private static readonly Vector2 BtnSize    = new Vector2(160f, 40f);
        private const           float   BtnH       = 40f;
        private const           float   BtnPadding = 20f;
        private const           float   TopPad     = 30f;

        // ── Dialog_TakeItems ─────────────────────────────────────────────────

        [HarmonyPatch(typeof(Dialog_TakeItems), nameof(Dialog_TakeItems.DoWindowContents))]
        [HarmonyPrefix]
        public static bool TakeItems_DoWindowContents(
            Dialog_TakeItems __instance,
            Rect inRect,
            TransferableOneWayWidget ___itemsTransfer,
            List<TransferableOneWay> ___transferables,
            Caravan ___caravan,
            Outpost ___outpost)
        {
            if (___itemsTransfer == null) return true;

            GUI.BeginGroup(inRect);
            Rect fullRect = inRect.AtZero();

            // Bottom buttons sit at the very bottom of fullRect
            float btnY   = fullRect.height - BtnH;
            Rect  takeR  = new Rect(fullRect.width - BtnSize.x, btnY, BtnSize.x, BtnH);
            Rect  cancelR = new Rect(0f,                          btnY, BtnSize.x, BtnH);
            Rect  resetR  = new Rect(fullRect.width / 2f - BtnSize.x, btnY, BtnSize.x, BtnH);

            // List rect: from TopPad down to just above the buttons
            Rect listRect = new Rect(0f, TopPad, fullRect.width,
                fullRect.height - TopPad - BtnH - BtnPadding);

            // Draw list first (behind buttons)
            ___itemsTransfer.OnGUI(listRect);

            // Draw buttons on top
            if (Widgets.ButtonText(takeR, "Outposts.Take".Translate()))
            {
                foreach (var transferable in ___transferables)
                    while (transferable.HasAnyThing && transferable.CountToTransfer > 0)
                    {
                        var thing = transferable.things.Pop();
                        if (thing.stackCount <= transferable.CountToTransfer)
                        {
                            transferable.AdjustBy(-thing.stackCount);
                            thing.holdingOwner?.Remove(thing);
                            ___caravan.AddPawnOrItem(___outpost.TakeItem(thing), true);
                        }
                        else
                        {
                            ___caravan.AddPawnOrItem(thing.SplitOff(transferable.CountToTransfer), true);
                            transferable.AdjustTo(0);
                            transferable.things.Add(thing);
                        }
                    }
                GUI.EndGroup();
                __instance.Close();
                return false;
            }

            if (Widgets.ButtonText(cancelR, "CancelButton".Translate()))
            {
                GUI.EndGroup();
                __instance.Close();
                return false;
            }

            if (Widgets.ButtonText(resetR, "ResetButton".Translate()))
            {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                AccessTools.Method(typeof(Dialog_TakeItems), "CalculateAndRecacheTransferables")
                    ?.Invoke(__instance, null);
            }

            GUI.EndGroup();
            return false;
        }

        // ── Dialog_GiveItems ─────────────────────────────────────────────────

        [HarmonyPatch(typeof(Dialog_GiveItems), nameof(Dialog_GiveItems.DoWindowContents))]
        [HarmonyPrefix]
        public static bool GiveItems_DoWindowContents(
            Dialog_GiveItems __instance,
            Rect inRect,
            TransferableOneWayWidget ___itemsTransfer,
            List<TransferableOneWay> ___transferables,
            Caravan ___caravan,
            Outpost ___outpost)
        {
            if (___itemsTransfer == null) return true;

            GUI.BeginGroup(inRect);
            Rect fullRect = inRect.AtZero();

            float btnY    = fullRect.height - BtnH;
            Rect  giveR   = new Rect(fullRect.width - BtnSize.x, btnY, BtnSize.x, BtnH);
            Rect  cancelR = new Rect(0f,                          btnY, BtnSize.x, BtnH);
            Rect  resetR  = new Rect(fullRect.width / 2f - BtnSize.x, btnY, BtnSize.x, BtnH);

            Rect listRect = new Rect(0f, TopPad, fullRect.width,
                fullRect.height - TopPad - BtnH - BtnPadding);

            ___itemsTransfer.OnGUI(listRect);

            if (Widgets.ButtonText(giveR, "Outposts.Give".Translate()))
            {
                foreach (var transferable in ___transferables)
                    while (transferable.HasAnyThing && transferable.CountToTransfer > 0)
                    {
                        var thing = transferable.things.Pop();
                        if (thing.stackCount <= transferable.CountToTransfer)
                        {
                            transferable.AdjustBy(-thing.stackCount);
                            thing.holdingOwner?.Remove(thing);
                            ___outpost.AddItem(thing);
                        }
                        else
                        {
                            ___outpost.AddItem(thing.SplitOff(transferable.CountToTransfer));
                            transferable.AdjustTo(0);
                            transferable.things.Add(thing);
                        }
                    }
                GUI.EndGroup();
                __instance.Close();
                return false;
            }

            if (Widgets.ButtonText(cancelR, "CancelButton".Translate()))
            {
                GUI.EndGroup();
                __instance.Close();
                return false;
            }

            if (Widgets.ButtonText(resetR, "ResetButton".Translate()))
            {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                AccessTools.Method(typeof(Dialog_GiveItems), "CalculateAndRecacheTransferables")
                    ?.Invoke(__instance, null);
            }

            GUI.EndGroup();
            return false;
        }
    }
}