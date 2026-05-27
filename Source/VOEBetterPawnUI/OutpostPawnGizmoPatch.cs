using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Outposts;
using RimWorld.Planet;
using Verse;

namespace VOEBetterPawnUI
{
    /// <summary>
    /// Patches Outpost.GetCaravanGizmos to replace the "Add pawn" FloatMenu
    /// with Dialog_ManagePawns, and Outpost.GetGizmos to replace the
    /// "Remove pawn" FloatMenu the same way.
    ///
    /// Both patches are PREFIX patches that return false, meaning they
    /// completely replace the relevant gizmo rather than running alongside it.
    /// We rebuild the full gizmo list so nothing else is lost.
    /// </summary>
    [HarmonyPatch]
    public static class OutpostPawnGizmoPatch
    {
        // ── Patch 1: GetCaravanGizmos ─────────────────────────────────────────
        // Replaces the FloatMenu in the "Add pawn" command with our dialog.
        [HarmonyPatch(typeof(Outpost), nameof(Outpost.GetCaravanGizmos))]
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> GetCaravanGizmos_Postfix(
            IEnumerable<Gizmo> __result,
            Outpost __instance,
            Caravan caravan)
        {
            foreach (var gizmo in __result)
            {
                // Identify the "Add pawn" command by its label key and swap it out
                if (gizmo is Command_Action cmd &&
                    cmd.defaultLabel == "Outposts.Commands.AddPawn.Label".Translate().ToString())
                {
                    yield return new Command_Action
                    {
                        action       = () => Find.WindowStack.Add(
                                           new Dialog_ManagePawns(__instance, caravan, Dialog_ManagePawns.Mode.Add)),
                        defaultLabel = cmd.defaultLabel,
                        defaultDesc  = cmd.defaultDesc,
                        icon         = cmd.icon
                    };
                }
                else
                {
                    yield return gizmo;
                }
            }
        }

        // ── Patch 2: GetGizmos ────────────────────────────────────────────────
        // Replaces the FloatMenu in the "Remove pawn" command with our dialog.
        [HarmonyPatch(typeof(Outpost), nameof(Outpost.GetGizmos))]
        [HarmonyPostfix]
        public static IEnumerable<Gizmo> GetGizmos_Postfix(
            IEnumerable<Gizmo> __result,
            Outpost __instance)
        {
            // We need an associated caravan to pass to the dialog.
            // When removing, the caravan is created fresh per pawn, so we pass null.
            foreach (var gizmo in __result)
            {
                if (gizmo is Command_Action cmd &&
                    cmd.defaultLabel == "Outposts.Commands.Remove.Label".Translate().ToString())
                {
                    yield return new Command_Action
                    {
                        action       = () => Find.WindowStack.Add(
                                           new Dialog_ManagePawns(__instance, null, Dialog_ManagePawns.Mode.Remove)),
                        defaultLabel = cmd.defaultLabel,
                        defaultDesc  = cmd.defaultDesc,
                        icon         = cmd.icon,
                        Disabled     = cmd.Disabled,
                        disabledReason = cmd.disabledReason
                    };
                }
                else
                {
                    yield return gizmo;
                }
            }
        }
    }
}
